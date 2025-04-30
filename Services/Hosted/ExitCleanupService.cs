using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Server.Data;
using Server.Services.Concrete;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class ExitCleanupService(
    ILogger<ExitCleanupService> logger,
    IServiceProvider serviceProvider,
    ExitFeeds exitFeeds)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(6));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Exit Cleanup Service is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Exit Cleanup Service is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        exitFeeds.ClearPastDates();

        await context.Exits
            .Where(e => e.Dates.All(d => d.Date < DateTimeOffset.UtcNow.Date))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}