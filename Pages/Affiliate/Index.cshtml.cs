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
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.Users
                .Include(u => u.EventPlace)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            Place = new EventPlaceModel(user.EventPlace);
            Images = _eventPlacePictureService.GetDownloadWithFilenameUrls(user.EventPlace);

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
            
            Console.WriteLine("here");
            
            if (user == null) return NotFound();
            
            Console.WriteLine("pre");
            await _eventPlacePictureService.DeleteAsync(user.EventPlace, pictureName);
            Console.WriteLine("post");
            user.EventPlace.Images.Remove(pictureName);
            Console.WriteLine("asdf");
            await _userManager.UpdateAsync(user);
            Console.WriteLine("asdf2");
            
            return RedirectToPage("/Affiliate/Index");
        }
    }
}
