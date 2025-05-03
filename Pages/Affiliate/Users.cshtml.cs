using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Pages.Affiliate;

public class Users(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IProfilePictureService profilePictureService,
    IWebHostEnvironment environment,
    ExitFeeds feed) : PageModel
{
    public List<(InternalUserQuery, string)> UsersList { get; set; }
    public string LocationName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.Users
            .Include(u => u.EventPlace)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null) return Unauthorized();
        
        var locationId = user.EventPlace.LocationId;

        var users = feed.GetFullFeed(locationId);
        
        LocationName = (await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locationId)).Name;

        var cacheCount = 0;
        if (environment.IsEnvironment("Sandbox"))
        {
            UsersList = users
                .Select(u => (u, "https://picsum.photos/200/200?random=" + ++cacheCount)).ToList();
        }
        else
        {
            UsersList = users.Select(u => (u, u.HasPfp ? profilePictureService.GetDownloadUrl(u.UserName) : profilePictureService.GetFallbackUrl())).ToList();
        }
        
        return Page();
    }
}