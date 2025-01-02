using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
    private class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        public JwtAuthorizeAttribute()
        {
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
        }
    }

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
            JwtTokenManager tokenManager, RegisterModel model) =>
        {
            if (model.UserName == "fallback")
                return Results.BadRequest("Fallback username is reserved.");
            
            if (model.BirthDate > DateTime.Today.AddYears(-16))
                return Results.BadRequest("User must be at least 16 years old.");

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Gender = model.Gender,
                BirthDate = model.BirthDate,
                Email = model.Email,
                HasPfp = false,
                EventStatus = new EventStatus()
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return Results.BadRequest(result.Errors);

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/login", async (UserManager<ApplicationUser> userManager, JwtTokenManager tokenManager,
            LoginModel model) =>
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return Results.BadRequest("Invalid login attempt.");

            var success = await userManager.CheckPasswordAsync(user, model.Password);
            if (!success) return Results.BadRequest("Invalid login attempt.");

            return Results.Ok(tokenManager.GenerateToken(user.UserName, user.Email, user.Id));
        });

        app.MapPost("/api/account/update_profile",
            [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ProfileModel model) =>
            {
                var user = await userManager.FindByNameAsync(context.User.Identity.Name);
                if (user == null) return Results.BadRequest("User not found.");

                user.Name = model.Name;
                user.IgHandle = model.IgHandle;
                user.Description = model.Description;
                user.Gender = model.Gender;

                await userManager.UpdateAsync(user);

                return Results.Ok();
            });

        app.MapGet("/api/account/info", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                return Results.Ok(new UserDto(user));
            });

        app.MapGet("/api/account/profile", async (UserManager<ApplicationUser> userManager, string username) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == username);

            return user == null ? Results.NotFound() : Results.Ok(new UserDto(user));
        });

        app.MapPost("/api/account/update_pfp", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
                IFormFile file,
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
            .DisableAntiforgery();

        app.MapPost("/api/account/remove_pfp", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            IProfilePictureService pfpService, HttpContext context) =>
        {
            var user = await userManager.GetUserAsync(context.User);

            if (!user.HasPfp) return Results.Ok();

            await pfpService.RemoveProfilePictureAsync(context.User.Identity.Name);

            user.HasPfp = false;
            await userManager.UpdateAsync(user);

            return Results.Ok();
        });
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
        app.MapPost("/api/register_event", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            HttpContext context,
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
        });

        app.MapPost("/api/cancel_event", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, HttpContext context) =>
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

                        uQuery.EventStatus.With.Remove(user.UserName);

                        await userManager.UpdateAsync(uQuery);
                    }
                }

                user.EventStatus.With = null;

                await userManager.UpdateAsync(user);

                return Results.Ok();
            });

        app.MapPost("/api/invite_to_event",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, string invited, HttpContext context) =>
            {
                var q = userManager.Users
                    .Include(u => u.EventStatus);

                var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (!invitor.EventStatus.Active) return Results.Unauthorized();

                if (invitor.EventStatus.With.Contains(invited)) return Results.Ok();

                var invitedUser = await q.FirstOrDefaultAsync(u => u.UserName == invited);
                if (invitedUser == null) return Results.BadRequest("User not found.");
                
                if (invitedUser.EventStatus is { Active: true, With.Count: > 0 })
                    return Results.BadRequest("User is already in an event.");

                invitedUser.EventStatus.Active = true;
                invitedUser.EventStatus.Time = invitor.EventStatus.Time;
                invitedUser.EventStatus.Location = invitor.EventStatus.Location;

                var userInvited = new List<string>(invitor.EventStatus.With);
                userInvited.Remove(invitedUser.UserName);
                userInvited.Add(invitor.UserName);
                invitedUser.EventStatus.With = userInvited;

                await userManager.UpdateAsync(invitedUser);

                foreach (var other in invitor.EventStatus.With)
                {
                    var otherUser = await q.FirstOrDefaultAsync(u => u.UserName == other);
                    otherUser.EventStatus.With.Add(invited);
                    await userManager.UpdateAsync(otherUser);
                }

                invitor.EventStatus.With.Add(invited);

                await userManager.UpdateAsync(invitor);
                return Results.Ok();
            });

        app.MapPost("/api/kick_from_event",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, string kicked, HttpContext context) =>
            {
                var q = userManager.Users
                    .Include(u => u.EventStatus);

                var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (!invitor.EventStatus.Active) return Results.Unauthorized();

                if (!invitor.EventStatus.With.Contains(kicked)) return Results.BadRequest();

                var kickedUser = await q.FirstOrDefaultAsync(u => u.UserName == kicked);
                if (kickedUser == null) return Results.BadRequest("User not found.");
                
                kickedUser.EventStatus.Active = false;
                kickedUser.EventStatus.Time = null;
                kickedUser.EventStatus.Location = null;
                kickedUser.EventStatus.With = null;
                
                foreach (var user in invitor.EventStatus.With)
                {
                    var userQuery = await q.FirstOrDefaultAsync(u1 => u1.UserName == user);
                    
                    if (userQuery.EventStatus.With == null) continue;
                    
                    userQuery.EventStatus.With.Remove(kicked);
                    await userManager.UpdateAsync(userQuery);
                }
                

                invitor.EventStatus.With.Remove(kicked);

                await userManager.UpdateAsync(kickedUser);
                await userManager.UpdateAsync(invitor);

                return Results.Ok();
            });

        app.MapGet("/api/query_visitors",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, HttpContext context, int page) =>
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
            });

        app.MapGet("/api/query_event_places", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IEventPlacePictureService pictureService, HttpContext context) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            var places = await dbContext.Places
                .Where(p => p.Location == user.EventStatus.Location)
                .Select(p =>
                    new
                    {
                        Place = p,
                        Offers = p.Offers.Where(o => o.ActiveOn.Date == user.EventStatus.Time.Value.Date).ToList()
                    }
                )
                .ToListAsync();

            var ret = places.Select(place =>
            {
                place.Place.Offers = place.Offers.Select((o, i) =>
                {
                    o.Image = o.Image == null ? null : pictureService.GetEventOfferPictureUrl(place.Place, i);
                    return o;
                }).ToList();
                return new EventPlaceDto(place.Place, pictureService.GetDownloadUrls(place.Place));
            }).ToList();

            return Results.Ok(ret);
        });
    }
}