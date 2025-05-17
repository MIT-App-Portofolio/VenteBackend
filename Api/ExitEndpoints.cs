using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.Dto;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Api;

public static class ExitEndpoints
{
    public static void MapExitEndpoints(WebApplication app)
    {
        app.MapGet("/api/exit/get_exits", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var results = await dbContext.Exits
                    .Where(e => e.Members.Contains(user.UserName) || e.Leader == user.UserName).Select(e =>
                        new ExitDto
                        {
                            Id = e.Id,
                            LocationId = e.LocationId,
                            Leader = e.Leader,
                            Name = e.Name,
                            Dates = e.Dates.Select(d => d.Date).ToList(),
                            AwaitingInvite = e.Invited,
                            Members = e.Members
                        }).ToListAsync();

                return Results.Ok(results);
            });

        app.MapGet("/api/exit/get_invites", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var results = await dbContext.Exits.Where(e => e.Invited.Contains(user.UserName)).Select(e =>
                    new ExitDto
                    {
                        Id = e.Id,
                        LocationId = e.LocationId,
                        Leader = e.Leader,
                        Name = e.Name,
                        Dates = e.Dates.Select(d => d.DateTime).ToList(),
                        AwaitingInvite = e.Invited,
                        Members = e.Members
                    }).ToListAsync();

                return Results.Ok(results);
            });

        app.MapPost("/api/exit/register", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                ExitFeeds feed, ExitRegisterModel model) =>
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(model);

                if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults);

                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var location = await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == model.LocationId);

                if (location == null) return Results.BadRequest("Unknown location");

                if (model.Dates.Count > 20)
                    return Results.BadRequest();

                if (model.NoTzTransform == null || !model.NoTzTransform.Value)
                {
                    // there's a bug on the frontend that selects the date in utc then translates it to local time which ends up selecting the day before
                    model.Dates = model.Dates.Select(d => d.AddHours(2)).ToList();
                }
                
                if (await DatesOverlap(dbContext, model.Dates, user.UserName))
                    return Results.BadRequest("date_overlap");

                var newExit = new ExitInstance
                {
                    Leader = user.UserName,
                    LocationId = model.LocationId,
                    Name = model.Name ?? location.Name,
                    Members = [],
                    Invited = [],
                    Likes = [],
                    Dates = model.Dates
                };
                
                await dbContext.Exits.AddAsync(newExit);
                await dbContext.SaveChangesAsync();

                feed.Enqueue(model.LocationId);

                return Results.Ok();
            });

        app.MapPost("/api/exit/invite", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NotificationService notificationService,
            int id, string userName) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

            if (exit == null || exit.Leader != user.UserName) return Results.BadRequest();

            var invited = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (invited == null) return Results.BadRequest("user_not_found");

            if (exit.Members.Contains(userName) || exit.Invited.Contains(userName) || exit.Leader == userName) return Results.BadRequest("user_already_in_exit");

            exit.Invited.Add(userName);

            await notificationService.SendInviteNotification(invited, user.UserName);

            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
            
        app.MapPost("/api/exit/kick", [JwtAuthorize] async (HttpContext context,
            UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
            int id, string userName) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

            if (exit == null || exit.Leader != user.UserName) return Results.BadRequest();

            var kicked = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (kicked == null) return Results.BadRequest();

            exit.Invited.Remove(kicked.UserName);
            exit.Members.Remove(kicked.UserName);

            if (exit.AlbumId != null)
            {
                var album = await dbContext.Albums.FirstAsync(a => a.Id == exit.AlbumId);
                album.Members.Remove(kicked.Id);
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });

        app.MapPost("/api/exit/accept_invite", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NotificationService notificationService,
                ExitFeeds feed, int id) =>
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

                if (exit == null || !exit.Invited.Contains(user.UserName)) return Results.BadRequest();

                if (await DatesOverlap(dbContext, exit.Dates, user.UserName))
                    return Results.BadRequest("date_overlap");
                
                var inviteTasks = new List<Task>();
                foreach (var member in (List<string>)[..exit.Members, exit.Leader])
                {
                    var u = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == member);
                    if (u == null) continue;
                    inviteTasks.Add(notificationService.SendInviteAcceptedNotification(u, user.UserName));
                }
                await Task.WhenAll(inviteTasks);

                exit.Invited.Remove(user.UserName);
                exit.Members.Add(user.UserName);

                if (exit.AlbumId != null)
                {
                    var album = await dbContext.Albums.FirstAsync(a => a.Id == exit.AlbumId);
                    album.Members.Add(user.Id);
                }

                await dbContext.SaveChangesAsync();

                feed.Enqueue(exit.LocationId);

                return Results.Ok();
            });

        app.MapPost("/api/exit/decline_invite", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                int id) =>
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

                if (exit == null || !exit.Invited.Contains(user.UserName)) return Results.BadRequest();

                exit.Invited.Remove(user.UserName);

                await dbContext.SaveChangesAsync();

                return Results.Ok();
            });

        app.MapPost("/api/exit/cancel", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                ExitFeeds feed, int id) =>
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

                if (exit == null || !(exit.Members.Contains(user.UserName) || exit.Leader == user.UserName))
                    return Results.BadRequest();

                if (exit.Leader == user.UserName)
                {
                    dbContext.Exits.Remove(exit);
                }
                else
                {
                    exit.Members.Remove(user.UserName);
                    if (exit.AlbumId != null)
                    {
                        var album = await dbContext.Albums.FirstAsync(a => a.Id == exit.AlbumId);
                        album.Members.Remove(user.UserName);
                    }
                }

                await dbContext.SaveChangesAsync();

                feed.Enqueue(exit.LocationId);

                return Results.Ok();
            });

        app.MapGet("/api/exit/query_visitors", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                ExitFeeds feed, int id, int page, int? ageRangeMin, int? ageRangeMax, Gender? gender) =>
            {
                const int pageSize = 10;
                
                var user = await userManager.Users
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.Unauthorized();

                var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

                if (exit == null || !(exit.Members.Contains(user.UserName) || exit.Leader == user.UserName))
                    return Results.BadRequest();

                return Results.Ok(
                    feed.GetFeed(exit.LocationId, exit.Dates, ageRangeMin, ageRangeMax, gender, user.Blocked, user.UserName)
                    .Skip(pageSize * page)
                    .Take(pageSize));
            });

        app.MapGet("/api/exit/query_event_places", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IEventPlacePictureService pictureService, int id) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();
            
            var exit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == id);

            if (exit == null || !(exit.Members.Contains(user.UserName) || exit.Leader == user.UserName))
                return Results.BadRequest();

            var places = await dbContext.Places
                .Include(p => p.Events)
                .ThenInclude(e => e.Offers)
                .Where(p => p.LocationId == exit.LocationId)
                .OrderByDescending(p => p.Score)
                .Select(p =>
                    new
                    {
                        Place = p,
                        Events = p.Events
                            .OrderBy(o => o.Time)
                            .Where(o => exit.Dates.Any(d => (o.Time - d).Days < 14))
                            .ToList()
                    }
                )
                .ToListAsync();

            var ret = places.Select(place =>
            {
                place.Place.Events = place.Events.Select((e, i) =>
                {
                    var index = place.Place.Events.IndexOf(e);
                    e.Image = e.Image == null ? null : pictureService.GetEventPictureUrl(place.Place, index);
                    return e;
                }).ToList();
                return new EventPlaceDto(place.Place, pictureService.GetDownloadUrls(place.Place));
            }).ToList();
            
            return Results.Ok(ret);
        });
    }

    private static async Task<bool> DatesOverlap(ApplicationDbContext dbContext, List<DateTimeOffset> dates,
        string userName)
    {
        var newDates = dates.Select(e => e.Date).ToList();
        return await dbContext.Exits.AnyAsync(e =>
            (e.Leader == userName || e.Members.Contains(userName)) &&
            e.Dates.Any(d => newDates.Contains(d.Date)));
    }
}