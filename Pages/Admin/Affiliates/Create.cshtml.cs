using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Pages.Admin.Affiliates
{
    public class CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        : PageModel
    {
        public void OnGet() { }

        [BindProperty]
        public CreateAffiliateModel Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (await context.Places.AnyAsync(p => p.Name == Input.EventPlaceName))
            {
                ModelState.AddModelError(string.Empty, "Event place with this name already exists.");
                return Page();
            }

            if (!context.Locations.Any(l => l.Id == Input.EventPlaceLocation))
                return BadRequest("Location not found");

            var user = new ApplicationUser
            {
                UserName = Input.UserName,
                Gender = Gender.Male,
                BirthDate = DateTime.MinValue,
                Blocked = [],
                Email = Input.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            if (!(await userManager.AddToRoleAsync(user, "Affiliate")).Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Could not add to affiliate role.");
                return Page();
            }

            var eventPlace = new EventPlace
            {
                Owner = user,
                OwnerId = user.Id,
                Name = Input.EventPlaceName,
                LocationId = Input.EventPlaceLocation,
                Description = Input.EventPlaceDescription,
                Images = [],
                PriceRangeBegin = Input.EventPlacePriceRangeBegin,
                PriceRangeEnd = Input.EventPlacePriceRangeEnd,
                Events = []
            };

            await context.Places.AddAsync(eventPlace);
            await context.SaveChangesAsync();

            return RedirectToPage("/Admin/Affiliates/Index");
        }
    }
}
