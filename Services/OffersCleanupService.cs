using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services;

public class OffersCleanupService(
    ILogger<OffersCleanupService> logger,
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
            .Include(p => p.Offers)
            .ToList();

        foreach (var place in places)
        {
            var toRemove = place.Offers.Select((o, i) => (o,i)).Where(v => v.o.ActiveOn < DateTime.Today).ToList();
            
            foreach (var (offer, i) in toRemove)
            {
                logger.LogInformation("Deleting offer image {0} for place {1}", offer.Name, place.Name);
                if (offer.Image != null)
                    imageService.DeleteEventOfferPictureAsync(place, i).Wait();
            }

            foreach (var (offer, _) in toRemove)
            {
                logger.LogInformation("Deleting offer {0} for place {1}", offer.Name, place.Name);
                place.Offers.Remove(offer);
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