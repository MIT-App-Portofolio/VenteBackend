using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Concrete;

namespace Server.Api;

public class LikeEndpoints
{
    public static void MapLikeEndpoints(WebApplication app)
    {
        app.MapPost("/api/like/like_profile", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, ApplicationDbContext dbContext, ExitFeeds feed, string username, int exitId) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            
            if (user == null) return Results.Unauthorized();

            var likedExit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == exitId);

            if (likedExit == null) return Results.NotFound();
            
            var likedUser = await userManager.Users
                                            .Include(u => u.Notifications)
                                            .FirstOrDefaultAsync(u => u.UserName == username);

            if (likedUser == null) return Results.NotFound();

            if (likedExit.Likes.ContainsKey(username))
            {
                if (likedExit.Likes[username].Contains(user.UserName)) return Results.BadRequest();
                likedExit.Likes[username].Add(user.UserName); 
            }
            else
            {
                likedExit.Likes[username] = [user.UserName];
            }
            
            feed.UpdateLike(true, likedExit.LocationId, username, exitId, user.UserName);

            if (user.UserName != username)
            {
                likedUser.Notifications.Add(new Notification
                {
                    Message = $"{user.Name ?? "@" + user.UserName} te ha dado like",
                    Read = false,
                    ReferenceUsername = user.UserName,
                    Timestamp = DateTimeOffset.UtcNow,
                    Type = NotificationType.Like
                });

                await notificationService.SendLikeNotification(likedUser, user.UserName);
            }
            
            await userManager.UpdateAsync(likedUser);
            await userManager.UpdateAsync(user);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
        
        app.MapPost("/api/like/unlike_profile", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, ApplicationDbContext dbContext, ExitFeeds feed, string username, int exitId) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            
            if (user == null) return Results.Unauthorized();

            var likedExit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == exitId);

            if (likedExit == null) return Results.NotFound();
            
            var likedUser = await userManager.Users
                                            .Include(u => u.Notifications)
                                            .FirstOrDefaultAsync(u => u.UserName == username);

            if (likedUser == null) return Results.NotFound();
            
            if (likedExit.Likes.ContainsKey(username))
            {
                likedExit.Likes[username].Remove(user.UserName); 
            }
            
            likedUser.Notifications.RemoveAll(n => n.ReferenceUsername == user.UserName && n.Type == NotificationType.Like);
            
            feed.UpdateLike(false, likedExit.LocationId, username, exitId, user.UserName);

            await userManager.UpdateAsync(user);
            await userManager.UpdateAsync(likedUser);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
    }
}