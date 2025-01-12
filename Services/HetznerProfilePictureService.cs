namespace Server.Services;

public class HetznerProfilePictureService(IConfiguration configuration, IHttpClientFactory clientFactory) : IProfilePictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    public Task UploadProfilePictureAsync(Stream pictureStream, string username)
    {
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, $"/profile-pictures/{username}.jpeg"), new StreamContent(pictureStream));
    }

    public string GetDownloadUrl(string username)
    {
        return new Uri(_pfpBucketUrl, $"/profile-pictures/{username}.jpeg").ToString();
    }

    public string GetFallbackUrl()
    {
        return new Uri(_pfpBucketUrl, "/profile-pictures/fallback.jpeg").ToString();
    }

    public Task RemoveProfilePictureAsync(string username)
    {
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, $"/profile-pictures/{username}.jpeg"));
    }

    private HttpClient GetClient()
    {
        return clientFactory.CreateClient("hetzner-storage");
    }
}