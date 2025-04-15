using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;

namespace Server.Services.Hosted;

public class AlbumCleanupService(
    ILogger<AlbumCleanupService> logger,
    IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Album Cleanup Service is starting.");
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pictureService = scope.ServiceProvider.GetRequiredService<IAlbumPictureService>();

        var albums = context.Albums.Where(a => a.DeletionDate <= DateTimeOffset.Now).Include(a => a.Pictures).ToList();

        var tasks = albums
            .SelectMany(a => a.Pictures.Select(p => pictureService.RemoveAlbumPicture(a.Id, p.Id)))
            .ToList();

        Task.WaitAll(tasks.ToArray());
        
        context.Albums.RemoveRange(albums);
        context.SaveChangesAsync();

        context.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Album Cleanup Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}