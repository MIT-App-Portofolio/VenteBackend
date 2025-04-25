using Server.Services.Concrete;

namespace Server.Services.Hosted;

public class ExitFeedExecutor(
    ILogger<ExitFeedExecutor> logger,
    IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Exit feed executor is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var feed = scope.ServiceProvider.GetRequiredService<ExitFeeds>();

        feed.ExecuteQueue().Wait();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Exit feed executor is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
