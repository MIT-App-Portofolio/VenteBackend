using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages.Admin
{
    public class ManageAffiliatesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageAffiliatesModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Affiliates = (await _userManager.GetUsersInRoleAsync("Affiliate")).ToList();

            return Page();
        }

        public List<ApplicationUser> Affiliates { get; set; }
    }
}
