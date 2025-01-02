using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;

namespace Server.Pages.Affiliate
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEventPlacePictureService _eventPlacePictureService;

        public IndexModel(UserManager<ApplicationUser> userManager, IEventPlacePictureService eventPlacePictureService)
        {
            _userManager = userManager;
            _eventPlacePictureService = eventPlacePictureService;
        }

        public EventPlaceModel Place { get; set; }
        public List<(string, string)> Images { get; set; }
        public List<EventPlaceOfferModel> Offers { get; set; }
        public List<string?> OfferPictures { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            Place = new EventPlaceModel(user.EventPlace);
            Images = _eventPlacePictureService.GetDownloadWithFilenameUrls(user.EventPlace);
            Offers = user.EventPlace.Offers.Select(o => new EventPlaceOfferModel(o)).ToList();
            OfferPictures = user.EventPlace.Offers
                .Select((o, i) => 
                    o.Image == null ? null : _eventPlacePictureService.GetEventOfferPictureUrl(user.EventPlace, i)).ToList();

            return Page();
        }
       
        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile picture)
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            await _eventPlacePictureService.UploadAsync(user.EventPlace, picture.OpenReadStream(), picture.FileName);
            
            user.EventPlace.Images.Add(picture.FileName);
            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(string pictureName)
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            await _eventPlacePictureService.DeleteAsync(user.EventPlace, pictureName);
            user.EventPlace.Images.Remove(pictureName);
            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
        
        [BindProperty]
        public EventPlaceOfferModel CreateOfferInput { get; set; }

        public async Task<IActionResult> OnPostCreateOfferAsync()
        {
            if (!ModelState.IsValid) return RedirectToPage("/Affiliate/Index");
            
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();

            if (user.EventPlace.Offers.Any(offer => offer.Name == CreateOfferInput.Name))
                return RedirectToPage("/Affiliate/Index");
            
            user.EventPlace.Offers.Add(new EventPlaceOffer
            {
                ActiveOn = CreateOfferInput.ActiveOn,
                Name = CreateOfferInput.Name,
                Description = CreateOfferInput.Description,
                Price = CreateOfferInput.Price
            });
            
            await _userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostDeleteOfferAsync(int offerId)
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            await _eventPlacePictureService.DeleteEventOfferPictureAsync(user.EventPlace, offerId);
            user.EventPlace.Offers.RemoveAt(offerId);
            await _userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostUploadOfferPictureAsync(int offerId, IFormFile picture)
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            await _eventPlacePictureService.UploadEventOfferPictureAsync(user.EventPlace, offerId, picture.OpenReadStream(), picture.FileName);
            
            user.EventPlace.Offers[offerId].Image = picture.FileName;
            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
        
        public async Task<IActionResult> OnPostDeleteOfferPictureAsync(int offerId)
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .ThenInclude(p => p.Offers)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            
            if (user == null) return NotFound();
            
            await _eventPlacePictureService.DeleteEventOfferPictureAsync(user.EventPlace, offerId);
            user.EventPlace.Offers[offerId].Image = null;
            await _userManager.UpdateAsync(user);
            
            return RedirectToPage("/Affiliate/Index");
        }
    }
}
