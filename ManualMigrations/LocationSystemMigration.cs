using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.ManualMigrations;

public class LocationSystemMigration
{
    public static async Task Migrate(ApplicationDbContext dbContext)
    {
        await dbContext.Places.ForEachAsync(p => p.LocationId = "salou");
        await dbContext.Users.Include(u => u.EventStatus)
            .ForEachAsync(u =>
            {
                u.EventStatus.Time = null;
                u.EventStatus.Active = false;
                u.EventStatus.LocationId = null;
                u.EventStatus.EventGroupId = null;
                u.EventStatus.EventGroupInvitationId = null;
            });
        await dbContext.SaveChangesAsync();
    }
}