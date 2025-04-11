using Server.Data;

namespace Server.Services.Hosted;

public class NoteCleanupService(
    ILogger<NoteCleanupService> logger,
    IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Note Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
        var usersWithExpiredEvents = context.Users
            .Where(u => u.CustomNote != null && u.NoteWasSet.Value <= DateTimeOffset.UtcNow.AddHours(-24))
            .ToList();

        foreach (var user in usersWithExpiredEvents)
        {
            logger.LogInformation("Cleaning up note for user {0}", user.Email);
            user.CustomNote = null;
            user.NoteWasSet = null;
        }

        context.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Note Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}