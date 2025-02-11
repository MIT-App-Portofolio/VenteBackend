using Bogus;
using Microsoft.AspNetCore.Identity;
using Server.Data;
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
    }
}