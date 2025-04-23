using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Pages.Admin;

public class UserStats(UserManager<ApplicationUser> userManager) : PageModel
{
    public int Users { get; set; }
    public int UsersCreatedToday { get; set; }
    
    public int Males { get; set; }
    public int Females { get; set; }
    public int Unspecified { get; set; }
   
    public int ActiveEvent { get; set; }
    
    public async Task OnGetAsync()
    {
        var allUsers = await userManager.Users
            .AsNoTracking()
            .Select(u => new { u.Gender, u.CreatedAt, u.EventStatus})
            .ToListAsync();

        var stats = allUsers.Aggregate(new {
            Total = 0,
            Male = 0,
            Female = 0,
            Other = 0,
            EventActive = 0,
            CreatedToday = 0,
        }, (acc, u) => new {
            Total = acc.Total + 1,
            Male = acc.Male + (u.Gender == Gender.Male ? 1 : 0),
            Female = acc.Female + (u.Gender == Gender.Female ? 1 : 0),
            Other = acc.Other + (u.Gender == Gender.NotSpecified ? 1 : 0), 
            EventActive = acc.EventActive + (u.EventStatus.Active ? 1 : 0),
            CreatedToday = acc.CreatedToday + (u.CreatedAt?.ToUniversalTime().Date == DateTimeOffset.UtcNow.Date ? 1 : 0),
        });

        Users = stats.Total;
        UsersCreatedToday = stats.CreatedToday;
        Males = stats.Male;
        Females = stats.Female;
        Unspecified = stats.Other;
        ActiveEvent = stats.EventActive;
    }
}