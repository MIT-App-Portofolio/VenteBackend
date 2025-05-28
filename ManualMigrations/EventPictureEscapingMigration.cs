using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.Interfaces;

namespace Server.ManualMigrations;

public class EventPictureEscapingMigration
{
    public static async Task Migrate(ApplicationDbContext dbContext, IEventPlacePictureService pictureService, IConfiguration configuration, IHttpClientFactory factory)
    {
        Uri pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
        using var httpClient = factory.CreateClient("hetzner-storage");
        foreach (var place in await dbContext.Places.Include(p => p.Events).ToListAsync())
        {
            for (var i = 0; i < place.Events.Count; i++)
            {
                var e = place.Events[i];
                if (e.Image == null) continue;

                var memoryStream = new MemoryStream();
                // var path = $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(@event.Name)}_{@event.Time.ToUnixTimeSeconds().ToString()}/{Uri.EscapeDataString(filename)}";
                var path = $"places-pictures/{place.Name}/{e.Name}_{e.Time.ToUnixTimeSeconds().ToString()}/{e.Image}";
                var uri = new Uri(pfpBucketUrl, path);

                try
                {
                    await using (var stream = await httpClient.GetStreamAsync(uri))
                    {
                        await stream.CopyToAsync(memoryStream);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine($"Could not download {place.Name} {e.Name}: {err}");
                    continue;
                }
                
                await httpClient.DeleteAsync(uri);
                memoryStream.Position = 0;
                await pictureService.UploadEventPictureAsync(place, i, memoryStream, e.Image);
                await memoryStream.DisposeAsync();
            }
        }
    }
}