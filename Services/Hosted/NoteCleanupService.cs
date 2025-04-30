using Server.Data;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class NoteCleanupService(
    ILogger<NoteCleanupService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Note Cleanup Service is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Note Cleanup Service is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
        var usersWithExpiredEvents = await context.Users
            .Where(u => u.CustomNote != null && u.NoteWasSet.Value <= DateTimeOffset.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);

        foreach (var user in usersWithExpiredEvents)
        {
            logger.LogInformation("Cleaning up note for user {0}", user.Email);
            user.CustomNote = null;
            user.NoteWasSet = null;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}