using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class EventsCleanupService(
    ILogger<EventsCleanupService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(6));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Offers Cleanup Service is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Offers Cleanup Service is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var imageService = scope.ServiceProvider.GetRequiredService<IEventPlacePictureService>();
        
        var places = await context.Places
            .Include(p => p.Events)
            .ToListAsync(cancellationToken);

        foreach (var place in places)
        {
            var toRemove = place.Events.Select((o, i) => (o,i)).Where(v => v.o.Time < DateTime.Today).ToList();
            
            foreach (var (e, i) in toRemove)
            {
                logger.LogInformation("Deleting offer image {0} for place {1}", e.Name, place.Name);
                if (e.Image != null)
                {
                    try
                    {
                        await imageService.DeleteEventPictureAsync(place, i);
                    }
                    catch (Exception error)
                    {
                        logger.LogError("Could not delete offer image {0} for place {1}: {2}", e.Name, place.Name, error.ToString());
                    }
                }
            }

            foreach (var (e, _) in toRemove)
            {
                logger.LogInformation("Deleting offer {0} for place {1}", e.Name, place.Name);
                place.Events.Remove(e);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}