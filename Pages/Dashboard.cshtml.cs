using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Server.Data;

namespace Server.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            Console.WriteLine(user);
            if (user == null)
                return Unauthorized();

            Console.WriteLine("a");

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToPage("/Admin/Index");

            Console.WriteLine("b");

            return RedirectToPage("/User/Index");
        }
    }
}
