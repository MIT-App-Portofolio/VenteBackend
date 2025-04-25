using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Concrete;

namespace Server.Services.Hosted;

public class EventStatusCleanupService(
    ILogger<EventStatusCleanupService> logger,
    IServiceProvider serviceProvider,
    ExitFeeds exitFeeds)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Event Status Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        exitFeeds.ClearPastDates();
            
        var usersWithExpiredEvents = context.Users
            .Include(u => u.EventStatus)
            .Where(u => u.EventStatus.Time != null && u.EventStatus.Time.Value < DateTime.Today)
            .ToList();

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

        context.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Event Status Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}