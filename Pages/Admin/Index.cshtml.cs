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
            var allUsers = await userManager.Users.ToListAsync();
            Users = allUsers.Count;
            UsersCreatedToday = 
                allUsers.Count(u => u.CreatedAt.HasValue && u.CreatedAt.Value.ToUniversalTime().Date == DateTimeOffset.UtcNow.Date);
        }
    }
}
