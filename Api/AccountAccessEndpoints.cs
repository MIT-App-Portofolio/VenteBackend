using System.Diagnostics;
using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;

namespace Server.Api;

public static class AccountAccessEndpoints
{
    public static void MapAccountAccessEndpoints(WebApplication app)
    {
        app.MapGet("/api/access_pfp",
            [JwtAuthorize] async (string userName, UserManager<ApplicationUser> userManager,
                IProfilePictureService pfpService, HttpContext context) =>
            {
                if (app.Environment.IsEnvironment("Sandbox"))
                {
                    if (userName != context.User.Identity.Name)
                        return Results.Ok(new Faker().Image.PicsumUrl(400, 400));
                }

                var user = await userManager.FindByNameAsync(userName);
                if (user == null) return Results.NotFound();

                if (!user.HasPfp)
                    return Results.Ok(pfpService.GetFallbackUrl());

                return Results.Ok(pfpService.GetDownloadUrl(userName));
            });
        

        app.MapGet("/api/account/profile",
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, string username) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (user == null) return Results.Unauthorized();

                List<string>? withUsernames = null;

                if (user.EventStatus.EventGroupId != null)
                {
                    var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupId);
                    if (group == null) throw new UnreachableException("Group could not be found.");

                    withUsernames = await userManager.Users.Where(u => group.Members.Contains(u.Id))
                        .Select(u => u.UserName!).ToListAsync();
                }

                return Results.Ok(new UserDto(user, withUsernames));
            });
    }
}