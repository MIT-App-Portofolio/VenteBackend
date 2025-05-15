using Server.Data;
using Server.Services.Interfaces;

namespace Server.Services.Implementations;

public class HetznerProfilePictureService(IConfiguration configuration, IHttpClientFactory clientFactory) : IProfilePictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    public Task UploadProfilePictureAsync(Stream pictureStream, string username)
    {
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, $"/profile-pictures/{username}.jpeg"), new StreamContent(pictureStream));
    }

    public string GetDownloadUrl(ApplicationUser user)
    {
        return new Uri(_pfpBucketUrl, $"/profile-pictures/{user.UserName}.jpeg?cache_v={user.PfpVersion}").ToString();
    }

    public string GetDownloadUrl(string username, int pfpVersion)
    {
        return new Uri(_pfpBucketUrl, $"/profile-pictures/{username}.jpeg?cache_v={pfpVersion}").ToString();
    }

    public string GetFallbackUrl()
    {
        return new Uri(_pfpBucketUrl, "/profile-pictures/fallback.jpeg").ToString();
    }

    public Task UploadReportPictureAsync(Stream pictureStream, string username, int pfpVersion)
    {
        return GetClient().PutAsync(new Uri(_pfpBucketUrl, $"/reports/{username}_{pfpVersion}.jpeg"), new StreamContent(pictureStream));
    }

    public Task DeleteReportPictureAsync(string username, int pfpVersion)
    {
        return GetClient().DeleteAsync(new Uri(_pfpBucketUrl, $"/reports/{username}_{pfpVersion}.jpeg"));
    }

    public string GetReportUrl(string userName, int pfpVersion)
    {
        return new Uri(_pfpBucketUrl, $"/reports/{userName}_{pfpVersion}.jpeg").ToString();
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