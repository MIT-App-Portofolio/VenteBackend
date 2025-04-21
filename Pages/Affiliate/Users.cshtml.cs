using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Pages.Affiliate;

public class Users(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IProfilePictureService profilePictureService,
    IWebHostEnvironment environment) : PageModel
{
    public List<(UserDto, string)> UsersList { get; set; }
    public string LocationName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var locationId = await userManager.Users
            .Include(u => u.EventPlace)
            .Where(u => u.UserName == User.Identity.Name)
            .Select(u => u.EventPlace.LocationId)
            .FirstOrDefaultAsync();

        var users = await userManager.Users
            .Include(u => u.EventStatus)
            .Where(u => !u.ShadowBanned)
            .Where(u => u.EventStatus.Active && u.EventStatus.LocationId == locationId)
            .OrderBy(u => u.EventStatus.Time)
            .ToListAsync();
        
        LocationName = (await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locationId)).Name;

        var cacheCount = 0;
        if (environment.IsEnvironment("Sandbox"))
        {
            UsersList = users
                .Select(u => (new UserDto(u, null), "https://picsum.photos/200/200?random=" + ++cacheCount)).ToList();
        }
        else
        {
            var dtos = await UserDto.FromListAsync(users, dbContext, userManager);
            UsersList = [];
            for (int i = 0; i < dtos.Count; i++)
            {
                UsersList.Add((dtos[i], users[i].HasPfp ? profilePictureService.GetDownloadUrl(users[i].UserName!) : profilePictureService.GetFallbackUrl()));
            }
        }
        
        return Page();
    }
}