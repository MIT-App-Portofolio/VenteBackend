using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users.Skip(PageId * 10).Take(10).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                // Handle error
                return BadRequest(result.Errors);
            }

            return RedirectToPage(new { pageId = PageId });
        }

        [FromRoute]
        public int PageId { get; set; }

        public List<ApplicationUser> Users { get; set; }
    }
}
