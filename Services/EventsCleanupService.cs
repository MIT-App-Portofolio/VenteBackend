using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public class EventsCleanupService(
    ILogger<EventsCleanupService> logger,
    IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Offers Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var imageService = scope.ServiceProvider.GetRequiredService<IEventPlacePictureService>();
        
        var places = context.Places
            .Include(p => p.Events)
            .ToList();

        foreach (var place in places)
        {
            var toRemove = place.Events.Select((o, i) => (o,i)).Where(v => v.o.Time < DateTime.Today).ToList();
            
            foreach (var (e, i) in toRemove)
            {
                logger.LogInformation("Deleting offer image {0} for place {1}", e.Name, place.Name);
                if (e.Image != null)
                    imageService.DeleteEventPictureAsync(place, i).Wait();
            }

            foreach (var (e, _) in toRemove)
            {
                logger.LogInformation("Deleting offer {0} for place {1}", e.Name, place.Name);
                place.Events.Remove(e);
            }
        }

        context.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Offers Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}