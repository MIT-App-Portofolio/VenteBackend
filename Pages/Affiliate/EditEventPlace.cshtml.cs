using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Pages.Affiliate
{
    public class EditEventPlaceModel(UserManager<ApplicationUser> userManager, ILogger<EditEventPlaceModel> logger) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.Users.Include(u => u.EventPlace).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            EventPlace = new EventPlaceModel(user.EventPlace);

            return Page();
        }

        [BindProperty]
        public EventPlaceModel EventPlace { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.Users.Include(u => u.EventPlace).FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                logger.LogInformation("Edit event place failed {0}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage + ", "));
                return Page();
            }

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

            await userManager.UpdateAsync(user);

            return RedirectToPage("/Affiliate/Index");
        }
    }
}
