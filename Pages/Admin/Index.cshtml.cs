using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin
{
    public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public int Users { get; set; }
        public int UsersCreatedToday { get; set; }
        public async Task OnGetAsync()
        {
            Users = await userManager.Users.CountAsync();
            UsersCreatedToday = await userManager.Users.Where(u =>
                    u.CreatedAt.HasValue && u.CreatedAt.Value.ToUniversalTime().Date == DateTimeOffset.UtcNow.Date)
                .CountAsync();
        }
    }
}
