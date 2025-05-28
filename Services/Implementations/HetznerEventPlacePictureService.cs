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
                new Uri(_pfpBucketUrl, NormalizeUrl($"places-pictures/{place.Name}/{image}")).ToString())
            .ToList();
    }

    public List<(string, string)> GetDownloadWithFilenameUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                (image, new Uri(_pfpBucketUrl, NormalizeUrl($"places-pictures/{place.Name}/{image}")).ToString()))
            .ToList();
    }

    public string GetEventPictureUrl(EventPlace place, int eventId)
    {
        var e = place.Events[eventId];
        // return new Uri(_pfpBucketUrl, $"places-pictures/{Uri.EscapeDataString(place.Name)}/{Uri.EscapeDataString(e.Name)}_{e.Time.ToUnixTimeSeconds().ToString()}/{Uri.EscapeDataString(e.Image)}").ToString();
        
        var url = new Uri(_pfpBucketUrl,
            NormalizeUrl($"places-pictures/{place.Name}/{e.Name}_{e.Time.ToUnixTimeSeconds().ToString()}/{e.Image}")).ToString();

        return url;
    }

    public Task UploadAsync(EventPlace place, Stream picture, string filename)
    {
        var path = NormalizeUrl($"places-pictures/{place.Name}/{filename}");
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task UploadEventPictureAsync(EventPlace place, int offerId, Stream picture, string filename)
    {
        var @event = place.Events[offerId];
        var path = NormalizeUrl($"places-pictures/{place.Name}/{@event.Name}_{@event.Time.ToUnixTimeSeconds().ToString()}/{filename}");
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task DeleteAsync(EventPlace place, string filename)
    {
        var path = NormalizeUrl($"places-pictures/{place.Name}/{filename}");
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    public Task DeleteEventPictureAsync(EventPlace place, int eventId)
    {
        var @event = place.Events[eventId];
        var path = NormalizeUrl($"places-pictures/{place.Name}/{@event.Name}_{@event.Time.ToUnixTimeSeconds().ToString()}/{@event.Image}");
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    private string NormalizeUrl(string url)
    {
        url = url.Replace("?", "qm");
        url = url.Replace("¿", "rqm");
        url = url.Replace("&", "and");
        return url;
    }

    private HttpClient GetClient()
    {
        return httpClientFactory.CreateClient("hetzner-storage");
    }
}