using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Pages.Affiliate
{
    public class EditEventPlaceModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EditEventPlaceModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.Users.Include(u => u.EventPlace).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            EventPlace = new EventPlaceModel(user.EventPlace);

            return Page();
        }

        [BindProperty]
        public EventPlaceModel EventPlace { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.Users.Include(u => u.EventPlace).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            if (!ModelState.IsValid) return Page();

            if (user.EventPlace == null)
            {
                user.EventPlace = new EventPlace();
            }

            user.EventPlace.Name = EventPlace.Name;
            user.EventPlace.Description = EventPlace.Description;
            user.EventPlace.PriceRangeBegin = EventPlace.PriceRangeStart;
            user.EventPlace.PriceRangeEnd = EventPlace.PriceRangeEnd;
            user.EventPlace.AgeRequirement = EventPlace.AgeRequirement;
            user.EventPlace.GoogleMapsLink = EventPlace.GoogleMapsLink;

            await _userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
    }
}
