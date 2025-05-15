using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Concrete;
using Server.Services.Interfaces;

namespace Server.Api;

public class FollowEndpoints
{
    public static void MapFollowEndpoints(WebApplication app)
    {
        app.MapGet("/api/follow/friends", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var users = await userManager.Users.Where(u => user.Friends.Contains(u.UserName)).Select(u => new UserSearchDto()
                {
                    Username = u.UserName,
                    PfpUrl = pfpService.GetDownloadUrl(u)
                }).ToListAsync();

                return Results.Ok(users);
            });
        
        app.MapPost("/api/follow/follow", [JwtAuthorize]
            async (string username, HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NotificationService notificationService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                if (username == user.UserName) return Results.BadRequest();

                var followed = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (followed == null) return Results.NotFound();

                if (followed.SolicitedFollowTo.Contains(user.UserName)) return Results.BadRequest();

                if (user.Friends.Contains(user.UserName)) return Results.Ok();
                
                if (user.SolicitedFollowTo.Contains(username)) return Results.BadRequest();

                if (followed.Blocked.Contains(user.UserName)) return Results.Unauthorized();

                user.SolicitedFollowTo.Add(followed.UserName);

                await notificationService.SendFollowRequestNotification(followed, user.UserName);

                await userManager.UpdateAsync(user);

                return Results.Ok();
            });

        app.MapPost("/api/follow/unfollow", [JwtAuthorize]
            async (string username, HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var followed = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (followed == null) return Results.NotFound();

                user.SolicitedFollowTo.Remove(followed.UserName);
                user.Friends.Remove(followed.UserName);
                followed.Friends.Remove(user.UserName);
                
                await userManager.UpdateAsync(user);
                await userManager.UpdateAsync(followed);

                return Results.Ok();
            });

        app.MapGet("/api/follow/get_outgoing_solicitations", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var users = await userManager.Users.Where(u => user.SolicitedFollowTo.Contains(u.UserName)).Select(u => new UserSearchDto()
                {
                    Username = u.UserName,
                    PfpUrl = pfpService.GetDownloadUrl(u)
                }).ToListAsync();

                return Results.Ok(users);
            });
        
        app.MapGet("/api/follow/get_incoming_solicitations", [JwtAuthorize]
            async (HttpContext context, UserManager<ApplicationUser> userManager, IProfilePictureService pfpService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var users = await userManager.Users.Where(u => u.SolicitedFollowTo.Contains(user.UserName)).Select(u => new UserSearchDto()
                {
                    Username = u.UserName,
                    PfpUrl = pfpService.GetDownloadUrl(u)
                }).ToListAsync();

                return Results.Ok(users);
            });
        
        app.MapPost("/api/follow/accept", [JwtAuthorize]
            async (string username, HttpContext context, UserManager<ApplicationUser> userManager, 
                NotificationService notificationService) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var followed = await userManager.Users.Include(u => u.Notifications)
                    .FirstOrDefaultAsync(u => u.UserName == username);

                if (followed == null) return Results.BadRequest();

                if (!followed.SolicitedFollowTo.Contains(user.UserName)) return Results.BadRequest();

                followed.SolicitedFollowTo.Remove(user.UserName);
                user.SolicitedFollowTo.Remove(username);
                followed.Friends.Add(user.UserName);
                user.Friends.Add(followed.UserName);

                followed.Notifications.Add(new Notification
                {
                    Message = $"{user.UserName} ha aceptado tu solicitud",
                    ReferenceUsername = user.UserName,
                    Type = NotificationType.FollowAccept
                });

                await notificationService.SendFollowAcceptNotification(followed, user.UserName);
                
                await userManager.UpdateAsync(followed);

                return Results.Ok();
            });
        
        app.MapPost("/api/follow/reject", [JwtAuthorize]
            async (string username, HttpContext context, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
                if (user == null) return Results.Unauthorized();

                var followed = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (followed == null) return Results.BadRequest();

                if (!followed.SolicitedFollowTo.Contains(user.UserName)) return Results.BadRequest();

                followed.SolicitedFollowTo.Remove(user.UserName);

                await userManager.UpdateAsync(followed);

                return Results.Ok();
            });
    }
}