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

        [FromQuery]
        public int PageId { get; set; }

        public List<ApplicationUser> Users { get; set; }
    }
}
