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
                likedExit.Likes[username].Add(username); 
            }
            else
            {
                likedExit.Likes[username] = [username];
            }
            
            feed.UpdateLike(true, likedExit.LocationId, username, exitId, user.UserName);

            if (user.UserName != username)
            {
                likedUser.Notifications.Add(new Notification
                {
                    Message = $"{user.Name ?? "@" + user.UserName} te ha dado like",
                    Read = false,
                    ReferenceUsername = user.UserName,
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
            
            if (likedExit.Likes.ContainsKey(username))
            {
                likedExit.Likes[username].Remove(username); 
            }
            
            feed.UpdateLike(false, likedExit.LocationId, username, exitId, user.UserName);

            await userManager.UpdateAsync(user);
            await dbContext.SaveChangesAsync();

            return Results.Ok();
        });
    }
}