using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.ManualMigrations;

public class EventPictureMigrations
{
    public static async Task Migrate(ApplicationDbContext dbContext, IEventPlacePictureService pictureService, IConfiguration configuration, IHttpClientFactory factory)
    {
        Uri pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
        using var httpClient = factory.CreateClient("hetzner-storage");
        foreach (var eventPlace in await dbContext.Places.Include(p => p.Events).ToListAsync())
        {
            for (var i = 0; i < eventPlace.Events.Count; i++)
            {
                var e = eventPlace.Events[i];
                if (e.Image == null) continue;

                var memoryStream = new MemoryStream();
                var path = $"places-pictures/{eventPlace.Name}/{e.Name}_{e.Time.ToString()}/{e.Image}";
                var uri = new Uri(pfpBucketUrl, path);
                await using (var stream = await httpClient.GetStreamAsync(uri))
                {
                    await stream.CopyToAsync(memoryStream);
                }

                memoryStream.Position = 0;
                await pictureService.UploadEventPictureAsync(eventPlace, i, memoryStream, e.Image);
                await memoryStream.DisposeAsync();
                await httpClient.DeleteAsync(uri);
            }
        }
    }
}