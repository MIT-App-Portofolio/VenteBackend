using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Pages.Affiliate
{
    public class IndexModel(
        UserManager<ApplicationUser> userManager,
        IEventPlacePictureService eventPlacePictureService)
        : PageModel
    {
        public EventPlaceModel Place { get; set; }
        public List<(string, string)> Images { get; set; }
        public List<(EventPlaceEventModel, int)> Events { get; set; }
        public Dictionary<int, int> EventsOfferCount { get; set; }
        public Dictionary<int, string?> EventPictures { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Events)
                .ThenInclude(e => e.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            Place = new EventPlaceModel(user.EventPlace);
            Images = eventPlacePictureService.GetDownloadWithFilenameUrls(user.EventPlace);
            Events = user.EventPlace.Events.Select(e => (new EventPlaceEventModel(e), e.Id)).ToList();

            EventsOfferCount = new Dictionary<int, int>();
            EventPictures = new Dictionary<int, string?>();
            for (var i = 0; i < user.EventPlace.Events.Count; i++)
            {
                var @event = user.EventPlace.Events[i];
                EventPictures.Add(@event.Id, @event.Image == null ? null : eventPlacePictureService.GetEventPictureUrl(user.EventPlace, i));
                EventsOfferCount.Add(@event.Id, @event.Offers.Count);
            }

            return Page();
        }
       
        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile picture)
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            if (user.EventPlace.Images.Contains(picture.FileName))
                return BadRequest("Ya hay una imagen con este nombre");
            
            await eventPlacePictureService.UploadAsync(user.EventPlace, picture.OpenReadStream(), picture.FileName);
            
            user.EventPlace.Images.Add(picture.FileName);
            await userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(string pictureName)
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            await eventPlacePictureService.DeleteAsync(user.EventPlace, pictureName);
            user.EventPlace.Images.Remove(pictureName);
            await userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
        
        [BindProperty]
        public EventPlaceEventModel CreateEventInput { get; set; }

        public async Task<IActionResult> OnPostCreateEventAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage("/Affiliate/Index");
            
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Events)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();

            user.EventPlace.Events.Add(new EventPlaceEvent
            {
                Time = CreateEventInput.Time,
                Name = CreateEventInput.Name,
                Description = CreateEventInput.Description,
                Image = null,
                Offers = []
            });
            
            await userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostDeleteEventAsync(int eventId)
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Events)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            var index = user.EventPlace.Events.FindIndex(e => e.Id == eventId);
            
            if (index == -1) return NotFound();
            
            await eventPlacePictureService.DeleteEventPictureAsync(user.EventPlace, index);
            user.EventPlace.Events.RemoveAt(index);
            await userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostUploadEventPictureAsync(int eventId, IFormFile picture)
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Events)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            var index = user.EventPlace.Events.FindIndex(e => e.Id == eventId);
            
            if (index == -1) return NotFound();
            
            await eventPlacePictureService.UploadEventPictureAsync(user.EventPlace, index, picture.OpenReadStream(), picture.FileName);
            
            user.EventPlace.Events[index].Image = picture.FileName;
            await userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostDeleteEventPictureAsync(int eventId)
        {
            var user = await userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Events)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            var index = user.EventPlace.Events.FindIndex(e => e.Id == eventId);
            
            if (index == -1) return NotFound();
            
            await eventPlacePictureService.DeleteEventPictureAsync(user.EventPlace, index);
            
            user.EventPlace.Events[index].Image = null;
            await userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
    }
}
