using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Interfaces;

namespace Server.Api;

public class UserSearchEndpoints
{
    public static void MapUserSearchEndpoints(WebApplication app)
    {
        app.MapGet("/api/user_search",
            [JwtAuthorize] async (string q, HttpContext context, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();
                
                var qNorm = q.ToLower();
                var users = await userManager.Users
                    .Select(u => new
                    {
                        u.Name,
                        u.UserName,
                        u.HasPfp,
                        u.PfpVersion,
                        u.Blocked
                    })
                    .Where(u => u.UserName != user.UserName)
                    .Where(u => !u.Blocked.Contains(user.UserName))
                    .Where(u =>
                        u.UserName.ToLower().Contains(qNorm) ||
                        (u.Name != null &&
                         u.Name.ToLower().Contains(qNorm)))
                    .Select(u => new UserSearchDto
                    {
                        Username = u.UserName,
                        PfpUrl = u.HasPfp ? pfpService.GetDownloadUrl(u.UserName, u.PfpVersion) : pfpService.GetFallbackUrl()
                    }).ToListAsync();

                return Results.Ok(users);
            });
    }
}