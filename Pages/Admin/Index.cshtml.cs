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
            var allUsers = await userManager.Users.Select(u => u.CreatedAt).ToListAsync();
            Users = allUsers.Count;
            UsersCreatedToday = 
                allUsers.Count(u => u.HasValue && u.Value.ToUniversalTime().Date == DateTimeOffset.UtcNow.Date);
        }
    }
}
