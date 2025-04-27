using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;

namespace Server.Api;

public class MessageEndpoints
{
    public static void MapMessageEndpoints(WebApplication app)
    {
        app.MapGet("/api/messages/get_messages", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var latestMessageIds = await dbContext.Messages
                .Where(m => m.From == user.UserName || m.To == user.UserName)
                .GroupBy(m => m.From == user.UserName ? m.To : m.From)
                .Select(g => g.OrderByDescending(m => m.Timestamp).First().Id)
                .ToListAsync();

            // Fetch the actual messages
            var latestMessages = await dbContext.Messages
                .Where(m => latestMessageIds.Contains(m.Id))
                .ToListAsync();
            
            return Results.Ok(latestMessages.OrderByDescending(m => m.Timestamp).Select(m => new MessageDto(m, user.UserName)));
        });
        
        app.MapGet("/api/messages/from_user", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, string username, int? lastMessageId) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var q = dbContext.Messages
                .Where(m => (m.From == user.UserName && m.To == username) ||
                            (m.To == user.UserName && m.From == username));

            if (lastMessageId != null)
                q = q.Where(m => m.Id < lastMessageId);

            var messages = await q
                .OrderByDescending(u => u.Timestamp)
                .Take(10)
                .Select(m => new MessageDto(m, user.UserName))
                .ToListAsync();
            
            return Results.Ok(messages);
        });
    }
}