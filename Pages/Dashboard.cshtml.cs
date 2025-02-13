using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages
{
    public class DashboardModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (await userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToPage("/Admin/Index");
            if (await userManager.IsInRoleAsync(user, "Affiliate"))
                return RedirectToPage("/Affiliate/Index");

            return RedirectToPage("/User/Index");
        }
    }
}
