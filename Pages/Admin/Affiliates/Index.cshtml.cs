using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin.Affiliates
{
    public class IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            Affiliates = (await userManager.GetUsersInRoleAsync("Affiliate")).ToList();

            return Page();
        }

        public List<ApplicationUser> Affiliates { get; set; }

        public async Task<IActionResult> OnPostLoginAsync(string affiliateId)
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == affiliateId);
            if (user == null) return BadRequest();
            await signInManager.SignInAsync(user, true);
            return RedirectToPage("/");
        }
    }
}
