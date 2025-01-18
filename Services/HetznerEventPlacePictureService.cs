using Server.Data;

namespace Server.Services;

public class HetznerEventPlacePictureService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IEventPlacePictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    
    public List<string> GetDownloadUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                new Uri(_pfpBucketUrl, $"places-pictures/{place.Name}/{image}").ToString())
            .ToList();
    }

    public List<(string, string)> GetDownloadWithFilenameUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                (image, new Uri(_pfpBucketUrl, $"places-pictures/{place.Name}/{image}").ToString()))
            .ToList();
    }

    public string GetEventPictureUrl(EventPlace place, int eventId)
    {
        var e = place.Events[eventId];
        return new Uri(_pfpBucketUrl, $"places-pictures/{place.Name}/{e.Name}/{e.Image}").ToString();
    }

    public Task UploadAsync(EventPlace place, Stream picture, string filename)
    {
        var path = $"places-pictures/{place.Name}/{filename}";
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task UploadEventPictureAsync(EventPlace place, int offerId, Stream picture, string filename)
    {
        var @event = place.Events[offerId];
        var path = $"places-pictures/{place.Name}/{@event.Name}/{filename}";
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, path), new StreamContent(picture));
    }

    public Task DeleteAsync(EventPlace place, string filename)
    {
        var path = $"places-pictures/{place.Name}/{filename}";
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    public Task DeleteEventPictureAsync(EventPlace place, int eventId)
    {
        var @event = place.Events[eventId];
        var path = $"places-pictures/{place.Name}/{@event.Name}/{@event.Image}";
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, path));
    }

    private HttpClient GetClient()
    {
        return httpClientFactory.CreateClient("hetzner-storage");
    }
}