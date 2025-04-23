using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin
{
    public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public void OnGet()
        {
        }
    }
}
