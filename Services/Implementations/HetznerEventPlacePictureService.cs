using Server.Data;
using Server.Services.Interfaces;

namespace Server.Services.Implementations;

public class HetznerEventPlacePictureService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IEventPlacePictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    
    public List<string> GetDownloadUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                new Uri(_pfpBucketUrl, $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(image)}").ToString())
            .ToList();
    }

    public List<(string, string)> GetDownloadWithFilenameUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                (image, new Uri(_pfpBucketUrl, $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(image)}").ToString()))
            .ToList();
    }

    public string GetEventPictureUrl(EventPlace place, int eventId)
    {
        var e = place.Events[eventId];
        // return new Uri(_pfpBucketUrl, $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(e.Name)}_{e.Time.ToUnixTimeSeconds().ToString()}/{Uri.EscapeDataString(e.Image)}").ToString();
        var filename = e.Image;
        if (filename == null) return "";
        filename = filename.Replace("?", "qm");
        filename = filename.Replace("¿", "rqm");
        filename = filename.Replace("&", "and");
        filename = filename.Replace("/", "slash");
        return new Uri(_pfpBucketUrl, $"places-pictures/{place.Name}/{e.Name}_{e.Time.ToUnixTimeSeconds().ToString()}/{filename}").ToString();
    }

    public Task UploadAsync(EventPlace place, Stream picture, string filename)
    {
        var path = $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(filename)}";
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task UploadEventPictureAsync(EventPlace place, int offerId, Stream picture, string filename)
    {
        var @event = place.Events[offerId];
        filename = filename.Replace("?", "qm");
        filename = filename.Replace("¿", "rqm");
        filename = filename.Replace("&", "and");
        filename = filename.Replace("/", "slash");
        
        var path = $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(@event.Name)}_{@event.Time.ToUnixTimeSeconds().ToString()}/{Uri.EscapeDataString(filename)}";
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task DeleteAsync(EventPlace place, string filename)
    {
        var path = $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(filename)}";
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    public Task DeleteEventPictureAsync(EventPlace place, int eventId)
    {
        var @event = place.Events[eventId];
        var filename = @event.Image;
        if (filename == null) return Task.CompletedTask;
        filename = filename.Replace("?", "qm");
        filename = filename.Replace("¿", "rqm");
        filename = filename.Replace("&", "and");
        filename = filename.Replace("/", "slash");
        var path = $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(@event.Name)}_{@event.Time.ToUnixTimeSeconds().ToString()}/{Uri.EscapeDataString(filename)}";
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    private HttpClient GetClient()
    {
        return httpClientFactory.CreateClient("hetzner-storage");
    }
}