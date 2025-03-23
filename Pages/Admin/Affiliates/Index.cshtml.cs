using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages.Admin.Affiliates
{
    public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            Affiliates = (await userManager.GetUsersInRoleAsync("Affiliate")).ToList();

            return Page();
        }

        public List<ApplicationUser> Affiliates { get; set; }
    }
}
