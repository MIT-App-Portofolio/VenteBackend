using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Api;

public class StatusEndpoints
{
    public static void MapStatusEndpoints(WebApplication app)
    {
        app.MapGet("/api/status/get_statuses", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ExitFeeds feeds, IProfilePictureService pfpService) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.Unauthorized();
            return Results.Ok(feeds.GetFriendStatuses(user.Friends, pfpService));
        });
    }
}