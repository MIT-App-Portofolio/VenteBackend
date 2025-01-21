using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;

namespace Server.Pages.Affiliate;

public class Users(UserManager<ApplicationUser> userManager, IProfilePictureService profilePictureService, IWebHostEnvironment environment) : PageModel
{
    public List<(UserDto, string)> UsersList { get; set; }
    public Location Location { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var location = await userManager.Users
            .Include(u => u.EventPlace)
            .Where(u => u.UserName == User.Identity.Name)
            .Select(u => u.EventPlace.Location)
            .FirstOrDefaultAsync();

        var users = await userManager.Users
            .Include(u => u.EventStatus)
            .Where(u => u.EventStatus.Active && u.EventStatus.Location == location)
            .OrderBy(u => u.EventStatus.Time)
            .ToListAsync();

        Location = location;

        var cacheCount = 0;
        if (environment.IsEnvironment("Sandbox"))
        {
            UsersList = users.Select(u => (new UserDto(u), "https://picsum.photos/200/200?random=" + ++cacheCount)).ToList();
        }
        else
        {
            UsersList = users.Select(u => (new UserDto(u), u.HasPfp ? profilePictureService.GetDownloadUrl(u.UserName) : profilePictureService.GetFallbackUrl())).ToList();
        }

        return Page();
    }
}