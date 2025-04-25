using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.ManualMigrations;

public class ExitSystemMigration
{
    public static async Task Migrate(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        var users = await userManager.Users.Include(u => u.EventStatus).Where(u => u.EventStatus.Active).ToListAsync();
        
        foreach (var user in users)
        {
            var es = user.EventStatus;

            var exit = new ExitInstance()
            {
                Name = (await dbContext.Locations.FirstAsync(l => l.Id == es.LocationId)).Name,
                Dates = [es.Time.Value],
                LocationId = es.LocationId,
                Leader = user.UserName,
                Invited = [],
                Members = []
            };

            dbContext.Exits.Add(exit);
        }

        await dbContext.SaveChangesAsync();
    }
}