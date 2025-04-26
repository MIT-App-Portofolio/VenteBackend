using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Server.Data;
using Server.Services.Concrete;

namespace Server.Services.Hosted;

public class ExitCleanupService(
    ILogger<ExitCleanupService> logger,
    IServiceProvider serviceProvider,
    ExitFeeds exitFeeds)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Exit Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        exitFeeds.ClearPastDates();

        context.Exits
            .Where(e => e.Dates.All(d => d.Date < DateTimeOffset.UtcNow.Date))
            .ExecuteDelete();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Exit Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}