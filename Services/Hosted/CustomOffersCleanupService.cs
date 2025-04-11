using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Services.Hosted;

public class CustomOffersCleanupService(
    ILogger<CustomOffersCleanupService> logger,
    IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Custom Offers Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.CustomOffers.Where(o => o.ValidUntil <= DateTimeOffset.UtcNow).ExecuteDelete();

        context.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Custom Offers Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
