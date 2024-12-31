using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.Dto;
using Server.Services;
using SixLabors.ImageSharp;

namespace Server;

public static class Api
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        MapAccountEndpoints(app);
        MapAccountAccessEndpoints(app);
        MapInfoEndpoints(app);
        MapEventEndpoints(app);
    }

    private static void MapAccountEndpoints(WebApplication app)
    {
        app.MapPost("/api/account/register", async (UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RegisterModel model) =>
        {
            if (model.UserName == "fallback")
                return Results.BadRequest("Fallback username is reserved.");
            
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                Email = model.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            await signInManager.SignInAsync(user, true);
            return Results.Ok();
        });

        app.MapPost("/api/account/login", async (UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, LoginModel model) =>
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return Results.BadRequest("Invalid login attempt.");

            var result =
                await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: true,
                    lockoutOnFailure: false);

            return !result.Succeeded ? Results.BadRequest("Invalid login attempt.") : Results.Ok();
        });

        app.MapPost("/api/account/update_profile",
            async (HttpContext context, UserManager<ApplicationUser> userManager, ProfileModel model) =>
            {
                var user = await userManager.FindByNameAsync(context.User.Identity.Name);
                if (user == null) return Results.BadRequest("User not found.");

                user.Name = model.Name;
                user.IgHandle = model.IgHandle;
                user.Description = model.Description;
                user.Gender = model.Gender;

                await userManager.UpdateAsync(user);

                return Results.Ok();
            }).RequireAuthorization();

        app.MapGet("/api/account/info", async (HttpContext context, UserManager<ApplicationUser> UserManager) =>
        {
            if (!context.User.Identity.IsAuthenticated) return Results.Unauthorized();

            var user = await UserManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            return Results.Ok(new UserDto(user));
        });

        app.MapPost("/api/account/update_pfp", async (UserManager<ApplicationUser> userManager, [FromForm] IFormFile file,
                HttpContext context, IProfilePictureService pfpService) =>
            {
                if (!file.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Only JPEG files are allowed.");
                
                if (file.Length > 3 * 1024 * 1024)
                    return Results.BadRequest("File size exceeds the 3MB limit.");

                await using (var stream = file.OpenReadStream())
                {
                    using (var image = await Image.LoadAsync(stream))
                        if (image.Width != image.Height)
                            return Results.BadRequest("Image must have a 1:1 aspect ratio.");
                }

                var user = await userManager.GetUserAsync(context.User);
                user.HasPfp = true;
                await userManager.UpdateAsync(user);

                await pfpService.UploadProfilePictureAsync(file.OpenReadStream(), context.User.Identity.Name);
                return Results.Ok();
            })
            .DisableAntiforgery()
            .RequireAuthorization();

        app.MapPost("/api/account/remove_pfp", async (UserManager<ApplicationUser> userManager,
            IProfilePictureService pfpService, HttpContext context) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            user.HasPfp = false;
            await userManager.UpdateAsync(user);

            await pfpService.RemoveProfilePictureAsync(context.User.Identity.Name);
            return Results.Ok();
        }).RequireAuthorization();
    }

    private static void MapAccountAccessEndpoints(WebApplication app)
    {
        app.MapGet("/api/access_pfp",
            async (string userName, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
            {
                var user = await userManager.FindByNameAsync(userName);
                if (user == null) return Results.NotFound();

                if (!user.HasPfp)
                    return Results.Ok(pfpService.GetFallbackUrl());

                return Results.Ok(pfpService.GetDownloadUrl(userName));
            });
    }

    private static void MapInfoEndpoints(WebApplication app)
    {
        app.MapGet("/api/get_locations",
            () => { return Enum.GetValues<Location>().Select(location => new LocationDto(location)).ToList(); });
    }

    private static void MapEventEndpoints(WebApplication app)
    {
        app.MapPost("/api/register_event", async (UserManager<ApplicationUser> userManager, HttpContext context,
            Location location, DateTime time) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.Unauthorized();

            if (time < DateTime.Today)
                return Results.BadRequest();

            user.EventStatus.Active = true;
            user.EventStatus.Time = time;
            user.EventStatus.Location = location;
            user.EventStatus.With = [];

            await userManager.UpdateAsync(user);

            return Results.Ok();
        }).RequireAuthorization();

        app.MapPost("/api/cancel_event", async (UserManager<ApplicationUser> userManager, HttpContext context) =>
        {
            var q = userManager.Users
                .Include(u => u.EventStatus);
            var user = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.Unauthorized();

            user.EventStatus.Active = false;
            user.EventStatus.Time = null;
            user.EventStatus.Location = null;

            if (user.EventStatus.With != null)
            {
                foreach (var u in user.EventStatus.With)
                {
                    var uQuery = await q.FirstOrDefaultAsync(u1 => u1.UserName == u);

                    if (uQuery == null) continue;

                    uQuery.EventStatus.With.Remove(u);

                    await userManager.UpdateAsync(uQuery);
                }
            }

            user.EventStatus.With = null;

            await userManager.UpdateAsync(user);

            return Results.Ok();
        }).RequireAuthorization();

        app.MapPost("/api/invite_to_event",
            async (UserManager<ApplicationUser> userManager, List<string> invited, HttpContext context) =>
            {
                var q = userManager.Users
                    .Include(u => u.EventStatus);

                var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (!invitor.EventStatus.Active) return Results.Unauthorized();

                foreach (var user in invited)
                {
                    var u = await q.FirstOrDefaultAsync(u => u.UserName == user);
                    if (u == null) return Results.BadRequest($"User {user} not found.");

                    u.EventStatus.Active = true;
                    u.EventStatus.Time = invitor.EventStatus.Time;
                    u.EventStatus.Location = invitor.EventStatus.Location;
                    var userInvited = new List<string>(invited);
                    userInvited.Remove(u.UserName);
                    userInvited.Add(invitor.UserName);
                    u.EventStatus.With = userInvited;

                    await userManager.UpdateAsync(u);
                }

                invitor.EventStatus.With = invited;

                await userManager.UpdateAsync(invitor);
                ;
                return Results.Ok();
            }).RequireAuthorization();

        app.MapGet("/api/query_visitors",
            async (UserManager<ApplicationUser> userManager, HttpContext context, int page) =>
            {
                const int pageSize = 4;

                var user = await userManager.Users.Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                if (!user.EventStatus.Active)
                    return Results.Ok(new List<ApplicationUser>());

                var users = await userManager.Users
                    .Include(u => u.EventStatus)
                    .Where(u => u.EventStatus.Active == true &&
                                u.EventStatus.Location == user.EventStatus.Location &&
                                u.EventStatus.Time.Value.Day == user.EventStatus.Time.Value.Day)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto(u))
                    .ToListAsync();

                return Results.Ok(users);
            }).RequireAuthorization();

        app.MapGet("/api/query_event_places", async (UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IEventPlacePictureService pictureService, HttpContext context) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            var places = await dbContext.Places
                .Where(p => p.Location == user.EventStatus.Location)
                .ToListAsync();

            var ret = places.Select(place => new EventPlaceDto(place, pictureService.GetDownloadUrls(place))).ToList();

            return Results.Ok(ret);
        }).RequireAuthorization();
    }
}