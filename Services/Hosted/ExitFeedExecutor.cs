using Server.Services.Concrete;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class ExitFeedExecutor(
    ILogger<ExitFeedExecutor> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Exit feed executor is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Exit feed executor is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<ExitFeeds>();

        await feed.ExecuteQueue();
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}
