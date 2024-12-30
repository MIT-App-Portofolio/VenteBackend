using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;
using Server.Models;

namespace Server.Pages.Admin.Affiliates
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CreateModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public void OnGet() { }

        [BindProperty]
        public CreateAffiliateModel Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.UserName,
                Gender = Gender.Male,
                Email = Input.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            if (!(await _userManager.AddToRoleAsync(user, "Affiliate")).Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Could not add to affiliate role.");
                return Page();
            }

            var eventPlace = new EventPlace
            {
                Owner = user,
                OwnerId = user.Id,
                Name = Input.EventPlaceName,
                Location = Input.EventPlaceLocation,
                Description = Input.EventPlaceDescription,
                Images = [],
                PriceRangeBegin = Input.EventPlacePriceRangeBegin,
                PriceRangeEnd = Input.EventPlacePriceRangeEnd
            };

            await _context.Places.AddAsync(eventPlace);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Admin/Affiliates/Index");
        }
    }
}
