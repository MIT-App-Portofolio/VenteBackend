using System.Diagnostics;
using Bogus;
using Bogus.DataSets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Api;

public static class EventEndpoints
{
    static EventEndpoints()
    {
    }

    public static void MapEventEndpoints(WebApplication app)
    {
        app.MapGet("/api/invite_status", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.BadRequest();

                if (user.EventStatus.EventGroupInvitationId == null)
                    return Results.Ok(new
                    {
                        Invited = false,
                        Invitors = new List<string>()
                    });

                var invitorIds =
                    (await dbContext.Groups.FindAsync(user.EventStatus.EventGroupInvitationId))?.Members.ToList();

                if (invitorIds == null)
                    return Results.BadRequest();

                var groupUsernames = await userManager.Users
                    .Where(u => invitorIds.Contains(u.Id))
                    .Select(u => u.UserName!)
                    .ToListAsync();

                var invitors = new List<UserDto>();
                foreach (var id in invitorIds)
                {
                    var u = await userManager.Users
                        .Include(u => u.EventStatus)
                        .FirstOrDefaultAsync(u => u.Id == id);

                    if (u == null) continue;

                    invitors.Add(new UserDto(u, groupUsernames));
                }

                return Results.Ok(new
                {
                    Invited = true,
                    Invitors = invitors
                });
            });

        app.MapPost("/api/accept_invite", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.BadRequest();

                if (user.EventStatus.EventGroupInvitationId == null)
                    return Results.BadRequest();

                var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupInvitationId);

                if (group == null)
                    throw new UnreachableException("Group could not be found.");

                if (group.SharedAlbumId != null)
                {
                    var album = await dbContext.Albums.FirstAsync(a => a.Id == group.SharedAlbumId);
                    album.Members.Add(user.Id);
                }

                var groupMember = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.Id == group.Members[0]);
                
                if (groupMember == null) throw new UnreachableException("Could not find any member in invited group.");

                user.EventStatus.Active = true;
                user.EventStatus.EventGroupId = group.Id;
                user.EventStatus.LocationId = groupMember.EventStatus.LocationId;
                user.EventStatus.Time = groupMember.EventStatus.Time;
                user.EventStatus.EventGroupInvitationId = null;
                await userManager.UpdateAsync(user);
                
                group.Members.Add(user.Id);
                group.AwaitingInvite.Remove(user.Id);

                await dbContext.SaveChangesAsync();

                return Results.Ok();
            });
        
        app.MapPost("/api/decline_invite", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.BadRequest();

                if (user.EventStatus.EventGroupInvitationId == null)
                    return Results.BadRequest();

                var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupInvitationId);

                if (group == null)
                    throw new UnreachableException("Group could not be found.");

                user.EventStatus.EventGroupInvitationId = null;
                group.AwaitingInvite.Remove(user.Id);

                await userManager.UpdateAsync(user);
                await dbContext.SaveChangesAsync();

                return Results.Ok();
            });
        
        app.MapGet("/api/group_status", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context) =>
            {
                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (user == null) return Results.BadRequest();

                if (user.EventStatus.EventGroupId == null)
                    return Results.Ok(new GroupStatusDto());

                var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupId);

                if (group == null)
                    throw new UnreachableException("Group could not be found.");

                return Results.Ok(await GroupStatusDto.FromGroupAsync(group, userManager));
            });

        app.MapPost("/api/register_event", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            HttpContext context,
            string location, DateTimeOffset time) =>
        {
            if (time.ToUniversalTime() < DateTime.UtcNow.Date)
                return Results.BadRequest("Bad time");

            if (!dbContext.Locations.Any(l => l.Id == location))
                return Results.BadRequest("Location not found");
            
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            
            if (user == null) throw new UnreachableException("Could not find user");

            if (user.EventStatus.Active)
                return Results.BadRequest("User already registered");

            user.EventStatus.Active = true;
            user.EventStatus.Time = time;
            user.EventStatus.LocationId = location;
            user.EventStatus.EventGroupId = null;
            user.EventStatus.EventGroupInvitationId = null;

            var exit = new ExitInstance
            {
                Name = location,
                LocationId = location,
                Dates = [time],
                Leader = user.UserName,
                Members = [],
                Invited = []
            };

            await dbContext.Exits.AddAsync(exit);
            await dbContext.SaveChangesAsync();

            user.EventStatus.AssociatedExitId = exit.Id;
            
            await userManager.UpdateAsync(user);

            return Results.Ok();
        });

        app.MapPost("/api/cancel_event", [JwtAuthorize]
            async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, HttpContext context) =>
            {
                var q = userManager.Users
                    .Include(u => u.EventStatus);
                var user = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();
                
                if (user.EventStatus.AssociatedExitId != null) 
                    await dbContext.Exits.Where(e => e.Id == user.EventStatus.AssociatedExitId).ExecuteDeleteAsync();

                user.EventStatus.AssociatedExitId = null;
                user.EventStatus.Active = false;
                user.EventStatus.Time = null;
                user.EventStatus.LocationId = null;

                if (user.EventStatus.EventGroupId != null)
                {
                    var group = await dbContext.Groups.FindAsync(user.EventStatus.EventGroupId);

                    if (group != null)
                    {
                        group.Members.Remove(user.Id);
                        if (group.Members.Count == 0)
                        {
                            foreach (var invited in group.AwaitingInvite)
                            {
                                var u = await q.FirstOrDefaultAsync(u => u.Id == invited);
                                if (u == null) continue;
                                u.EventStatus.EventGroupInvitationId = null;
                            }

                            dbContext.Groups.Remove(group);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }

                // Technically, this is unreachable from the UI, if you have a pending invite you cannot navigate to the event page.
                // But if the user managed to bypass it then they will just see the invitation when they reopen the app, so we shouldn't handle this edge case.
                if (user.EventStatus.EventGroupInvitationId != null)
                {
                }

                user.EventStatus.EventGroupId = null;

                await userManager.UpdateAsync(user);

                return Results.Ok();
            });

        app.MapPost("/api/invite_to_event",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NotificationService notificationService,
                string invited, HttpContext context) =>
            {
                return Results.BadRequest();
                
                var q = userManager.Users
                    .Include(u => u.EventStatus);

                var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

                if (invitor.UserName == invited)
                    return Results.BadRequest();

                if (!invitor.EventStatus.Active) return Results.Unauthorized();

                var invitedUser = await q.FirstOrDefaultAsync(u => u.UserName == invited);
                if (invitedUser == null) return Results.BadRequest("User not found.");
                
                if (invitedUser.EventStatus.EventGroupInvitationId != null &&
                    invitedUser.EventStatus.EventGroupInvitationId == invitor.EventStatus.EventGroupId)
                    return Results.BadRequest("User already invited.");

                if (invitedUser.EventStatus.EventGroupId != null &&
                    invitedUser.EventStatus.EventGroupId == invitor.EventStatus.EventGroupId)
                    return Results.BadRequest("User already in group.");

                if (invitedUser.EventStatus.EventGroupInvitationId != null
                    /* you CAN invite users in another group. Especially those users who invited someone (got group created)
                     those declined, and now they are in a group with themselves
                     || invitedUser.EventStatus.EventGroupId != null */)
                    return Results.BadRequest("User already invited to another group.");

                if (invitor.EventStatus.EventGroupId == null)
                {
                    var group = new EventGroup
                    {
                        Members = [invitor.Id],
                        AwaitingInvite = []
                    };
                    await dbContext.AddAsync(group);
                    await dbContext.SaveChangesAsync();
                    invitor.EventStatus.EventGroupId = group.Id;
                }

                invitedUser.EventStatus.EventGroupInvitationId = invitor.EventStatus.EventGroupId;
                
                await userManager.UpdateAsync(invitedUser);
                await userManager.UpdateAsync(invitor);

                var existingGroup = await dbContext.Groups.FindAsync(invitor.EventStatus.EventGroupId) ??
                          throw new UnreachableException("Group could not be found.");

                existingGroup.AwaitingInvite.Add(invitedUser.Id);
                await dbContext.SaveChangesAsync();
                
                await notificationService.SendInviteNotification(invitedUser, invitor.UserName);

                return Results.Ok();
            });

        app.MapPost("/api/kick_from_event",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                string kicked, HttpContext context) =>
            {
                var q = userManager.Users
                    .Include(u => u.EventStatus);

                var invitor = await q.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                
                var kickedUser = await q.FirstOrDefaultAsync(u => u.UserName == kicked);

                if (kickedUser == null) return Results.BadRequest("User not found.");

                if (kickedUser.UserName == invitor.UserName)
                    return Results.BadRequest();

                if (!invitor.EventStatus.Active) return Results.Unauthorized();

                if (invitor.EventStatus.EventGroupId == null)
                    return Results.BadRequest();

                var group = await dbContext.Groups.FindAsync(invitor.EventStatus.EventGroupId);

                if (!group.Members.Contains(kickedUser.Id) && !group.AwaitingInvite.Contains(kickedUser.Id))
                    return Results.BadRequest();

                if (group.SharedAlbumId != null)
                {
                    var album = await dbContext.Albums.FirstAsync(a => a.Id == group.SharedAlbumId);
                    album.Members.Remove(kickedUser.Id);
                }

                if (!group.Members.Remove(kickedUser.Id))
                {
                    group.AwaitingInvite.Remove(kickedUser.Id);
                    kickedUser.EventStatus.EventGroupInvitationId = null;
                }

                kickedUser.EventStatus.Active = false;
                kickedUser.EventStatus.LocationId = null;
                kickedUser.EventStatus.Time = null;
                // Should not do this. If user is in group A, but invited to B, once kicked from A the invite from B should persist
                // kickedUser.EventStatus.EventGroupInvitationId = null;
                kickedUser.EventStatus.EventGroupId = null;

                await userManager.UpdateAsync(kickedUser);
                await userManager.UpdateAsync(invitor);
                await dbContext.SaveChangesAsync();

                return Results.Ok();
            });
        
        if (app.Environment.IsEnvironment("Sandbox"))
            MapMockupQueryEndpoints(app);
        else
            MapProdQueryEndpoints(app);
    }

    private static void MapProdQueryEndpoints(WebApplication app)
    {
        app.MapGet("/api/query_visitors",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext,
                HttpContext context, int page, int? ageRangeMin, int? ageRangeMax, Gender? gender) =>
            {
                const int pageSize = 150;

                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                if (!user.EventStatus.Active)
                    return Results.Ok(new List<ApplicationUser>());

                var query = userManager.Users
                    .Include(u => u.EventStatus)
                    .Where(u => !u.ShadowBanned)
                    .Where(u => !(!u.HasPfp && u.IgHandle == null))
                    .Where(u => u.EventStatus.Active == true &&
                                u.EventStatus.LocationId == user.EventStatus.LocationId &&
                                (u.EventStatus.Time.Value - user.EventStatus.Time.Value).Days < 14);
                
                if (user.Blocked != null)
                    query = query.Where(u => !user.Blocked.Contains(u.UserName));

                if (gender.HasValue)
                    query = query.Where(u => u.Gender == gender.Value);

                if (ageRangeMin.HasValue)
                {
                    var minDate = DateTime.Now.AddYears(-ageRangeMin.Value);
                    query = query.Where(u => u.BirthDate.HasValue && u.BirthDate <= minDate);
                }

                if (ageRangeMax.HasValue)
                {
                    var maxDate = DateTime.Now.AddYears(-ageRangeMax.Value);
                    query = query.Where(u => u.BirthDate.HasValue && u.BirthDate >= maxDate);
                }

                var users = await query
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                users = users
                    .OrderBy(u => Math.Abs((u.EventStatus.Time.Value.Date - user.EventStatus.Time.Value.Date).Ticks))
                    .ThenByDescending(u => u.HasPfp).ToList();

                return Results.Ok(await UserDto.FromListAsync(users, dbContext, userManager));
            });

        app.MapGet("/api/query_event_places", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IEventPlacePictureService pictureService, HttpContext context) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.BadRequest();

            var places = await dbContext.Places
                .Include(p => p.Events)
                .ThenInclude(e => e.Offers)
                .Where(p => p.LocationId == user.EventStatus.LocationId)
                .Select(p =>
                    new
                    {
                        Place = p,
                        Events = p.Events.OrderBy(o => o.Time).Where(o => (o.Time - user.EventStatus.Time.Value).Days < 14).ToList()
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

    private static void MapMockupQueryEndpoints(WebApplication app)
    {
        app.MapGet("/api/query_visitors",
            [JwtAuthorize] async (UserManager<ApplicationUser> userManager, 
                HttpContext context, int page, int? ageRangeMin, int? ageRangeMax, Gender? gender) =>
            {
                const int pageSize = 4;

                var user = await userManager.Users
                    .Include(u => u.EventStatus)
                    .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                if (!user.EventStatus.Active)
                    return Results.Ok(new List<UserDto>());

                var seed = user.EventStatus.Time.Value.Ticks + page;
               
                // to prevent infinite scrolling, limit to 5 pages only
                return Results.Ok(page > 5 ? [] : CreateRandomUsers(pageSize));

                List<UserDto> CreateRandomUsers(int count)
                {
                    var faker = new Faker<UserDto>("es")
                        .UseSeed((int)seed)
                        .RuleFor(u => u.Name, f => f.Name.FirstName() + " " + f.Name.LastName())
                        .RuleFor(u => u.Description, f => f.Lorem.Sentence())
                        .RuleFor(u => u.Gender, f => gender ?? (Gender)f.PickRandom(0, 1))
                        .RuleFor(u => u.Years, f =>
                        {
                            var start = 18;
                            var end = 30;
                            if (ageRangeMax.HasValue)
                                end = Math.Min(end, ageRangeMax.Value);
                            if (ageRangeMin.HasValue)
                                start = Math.Max(start, ageRangeMin.Value);

                            return f.Random.Int(start, end);
                        });

                    var ret = faker.Generate(count);
                    ret.ForEach(u =>
                    {
                        var username = u.Name.Replace(" ", "");
                        u.IgHandle = username;
                        u.UserName = username;
                        u.EventStatus = new EventStatusDto
                        {
                            Active = true,
                            Time = user.EventStatus.Time.Value,
                            LocationId = user.EventStatus.LocationId
                        };
                    });

                    if (user.Blocked != null)
                        ret.RemoveAll(u => user.Blocked.Contains(u.UserName));

                    return ret;
                }
            });

        app.MapGet("/api/query_event_places", [JwtAuthorize] async (UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext, IEventPlacePictureService pictureService, HttpContext context) =>
        {
            var user = await userManager.Users
                .Include(u => u.EventStatus)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.BadRequest();

            var seed = user.EventStatus.Time.Value.Ticks;

            var ret = CreateRandomPlaces(15);

            var randomCount = 0;
            foreach (var place in ret)
            {
                if (place.ImageUrls.Count == 0)
                    place.ImageUrls =
                    [
                        "https://picsum.photos/640/480?random=" + randomCount++,
                        "https://picsum.photos/640/480?random=" + randomCount++,
                        "https://picsum.photos/640/480?random=" + randomCount++
                    ];

                foreach (var e in place.Events)
                    e.Image ??= "https://picsum.photos/200/400?random=" + randomCount++;
            }

            return Results.Ok(ret);

            DateTimeOffset RandomDate()
            {
                var random = new Random();
                var start = user.EventStatus.Time.Value - TimeSpan.FromDays(2);
                var end = user.EventStatus.Time.Value + TimeSpan.FromDays(14);
                var range = (end - start).Days;
                return start.AddDays(random.Next(range));
            }

            List<EventPlaceDto> CreateRandomPlaces(int count)
            {
                var faker = new Faker<EventPlaceDto>("es")
                    .UseSeed((int)seed)
                    .RuleFor(p => p.Name, f => f.Company.CompanyName())
                    .RuleFor(p => p.ImageUrls, _ => [])
                    .RuleFor(p => p.Description, f => f.Lorem.Sentence())
                    .RuleFor(p => p.LocationId, _ => user.EventStatus.LocationId)
                    .RuleFor(p => p.PriceRangeBegin, f => f.Random.Int(5, 20))
                    .RuleFor(p => p.PriceRangeEnd, (f, p) => p.PriceRangeBegin + f.Random.Int(5, 20))
                    .RuleFor(p => p.AgeRequirement, f => f.Random.Bool() ? f.PickRandom(16, 18) : null)
                    .RuleFor(p => p.Events, f =>
                    {
                        var events = new List<EventPlaceEventDto>();
                        for (var i = 0; i < 3; i++)
                        {
                            var offers = new List<EventPlaceOfferDto>();

                            for (var j = 0; j < f.Random.Int(1, 5); j++)
                            {
                                offers.Add(new EventPlaceOfferDto
                                {
                                    Name = f.Commerce.ProductName(),
                                    Description = f.Lorem.Sentence(),
                                    Price = f.Random.Int(10, 50),
                                });
                            }

                            events.Add(new EventPlaceEventDto
                            {
                                Time = RandomDate().DateTime,
                                Name = f.Commerce.ProductName(),
                                Description = f.Lorem.Sentence(),
                                Offers = offers
                            });
                        }
                        events.Sort((e1, e2) => e1.Time.CompareTo(e2.Time));

                        return events;
                    });

                return faker.Generate(count);
            }
        });
    }
}