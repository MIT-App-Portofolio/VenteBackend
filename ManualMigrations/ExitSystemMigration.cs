using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.ManualMigrations;

public class ExitSystemMigration
{
    public static async Task Migrate(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, ILogger<ExitSystemMigration> logger)
    {
        var users = await userManager.Users.Include(u => u.EventStatus).Where(u => u.EventStatus.Active).ToListAsync();
        
        foreach (var user in users)
        {
            var es = user.EventStatus;

            if (es.AssociatedExitId != null)
            {
                var fetchedExit = await dbContext.Exits.FirstOrDefaultAsync(e => e.Id == es.AssociatedExitId);
                logger.LogInformation("User {0} with event status {1} {2} has associated exit. (exit exists: {3}). Skipping...", user.UserName, es.Time.Value.DateTime.DateShortDisplay(), es.LocationId, fetchedExit != null);
                continue;
            }

            var exit = new ExitInstance
            {
                Name = (await dbContext.Locations.FirstAsync(l => l.Id == es.LocationId)).Name,
                Dates = [es.Time.Value],
                LocationId = es.LocationId,
                Leader = user.UserName,
                Likes = [],
                Invited = [],
                Members = [],
                AttendingEvents = []
            };

            if (dbContext.Exits.Any(e =>
                    e.Name == exit.Name && e.Leader == exit.Leader && e.LocationId == exit.LocationId &&
                    e.Dates.Count == 1 && e.Dates[0] == exit.Dates[0] && e.Invited.Count == 0 &&
                    e.Members.Count == 0))
            {
                logger.LogInformation("User {0} with event status {1} {2} and without exit id has matched exit. Skipping...", user.UserName, es.Time.Value.DateTime.DateShortDisplay(), es.LocationId);
                continue;
            }
            
            logger.LogInformation("Migrating to exit for user {0}", user.UserName);
            dbContext.Exits.Add(exit);
        }

        await dbContext.SaveChangesAsync();
    }
}