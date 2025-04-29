using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;

namespace Server.Api;

public class NotificationEndpoints
{
    public static void MapNotificationEndpoints(WebApplication app)
    {
        app.MapGet("/api/notifications/get_all", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.Users
                .Select(u => new {u.UserName, u.Notifications})
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            
            if (user == null) return Results.Unauthorized();

            return Results.Ok(user.Notifications.Select(n => new NotificationDto(n)).ToList());
        });
        
        app.MapPost("/api/notifications/mark_read", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.Users
                .Include(u => u.Notifications)
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            
            if (user == null) return Results.Unauthorized();
            
            user.Notifications.ForEach(n => n.Read = true);

            await userManager.UpdateAsync(user);

            return Results.Ok();
        });
    }
}