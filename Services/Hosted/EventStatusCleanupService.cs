using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Concrete;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class EventStatusCleanupService(
    ILogger<EventStatusCleanupService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(6));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Event Status Cleanup Service is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Event Status Cleanup Service is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var usersWithExpiredEvents = await context.Users
            .Include(u => u.EventStatus)
            .Where(u => u.EventStatus.Time != null && u.EventStatus.Time.Value < DateTime.Today)
            .ToListAsync(cancellationToken);

        foreach (var user in usersWithExpiredEvents)
        {
            logger.LogInformation("Cleaning up event status for user {0}", user.Email);
                
            user.EventStatus = new EventStatus
            {
                Active = false,
                Time = null,
                LocationId = null,
            };
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}