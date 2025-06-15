using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;

namespace Server.Api;

public class GroupMessageEndpoints
{
    public static void MapGroupMessageEndpoints(WebApplication app)
    {
        app.MapGet("/api/group_messages/get_current", async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext) =>
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);
            if (user == null) return Results.BadRequest();

            var exits = await dbContext.Exits
                .Where(e => e.Members.Contains(user.UserName) || e.Leader == user.UserName)
                .Where(e => e.Members.Count > 0)
                .ToListAsync();

            var ids = exits.Select(e => e.Id).ToList();

            var messages = await dbContext.GroupMessages
                .Where(g => ids.Contains(g.ExitId))
                .GroupBy(g => g.ExitId)
                .Select(g => g.OrderByDescending(m => m.Timestamp).FirstOrDefault())
                .Select(m => m == null ? null : new GroupMessageSummaryDto(m, exits.First(e => e.Id == m.ExitId)))
                .ToListAsync();

            return Results.Ok(messages.Where(m => m != null).ToList());
        });
        
        app.MapGet("/api/group_messages/from_exit", [JwtAuthorize] async (HttpContext context, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, int exitId, int? lastMessageId) =>
        {
            var user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == context.User.Identity.Name);

            if (user == null) return Results.Unauthorized();

            var exit = await dbContext.Exits.FirstOrDefaultAsync(e =>
                e.Id == exitId && (e.Members.Contains(user.UserName) || e.Leader == user.UserName));
            
            if (exit == null) return Results.BadRequest();

            var q = dbContext.GroupMessages
                .Where(m => m.ExitId == exitId);

            if (lastMessageId != null)
                q = q.Where(m => m.Id < lastMessageId);

            var messages = await q
                .OrderByDescending(u => u.Timestamp)
                .Take(30)
                .Select(m => new GroupMessageDto(m))
                .ToListAsync();
            
            return Results.Ok(messages);
        });
    }
}