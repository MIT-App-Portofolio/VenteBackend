using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Server.Services.Hosted;

public class AlbumCleanupService(
    ILogger<AlbumCleanupService> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Album Cleanup Service is starting.");
        
        try
        {
            do
            {
                await DoWork(stoppingToken);
            } while (await _timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Album Cleanup Service is stopping.");
        }
    }

    private async Task DoWork(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pictureService = scope.ServiceProvider.GetRequiredService<IAlbumPictureService>();

        var albums = await context.Albums
            .Where(a => a.DeletionDate <= DateTimeOffset.UtcNow)
            .Include(a => a.Pictures)
            .ToListAsync(cancellationToken);

        var tasks = albums
            .SelectMany(a => a.Pictures.Select(p => pictureService.RemoveAlbumPicture(a.Id, p.Id)))
            .ToList();

        await Task.WhenAll(tasks);
        
        context.Albums.RemoveRange(albums);
        await context.SaveChangesAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
    }
}