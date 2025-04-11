using Server.Services.Interfaces;

namespace Server.Services.Implementations;

public class HetznerLocationImageService(IHttpClientFactory clientFactory, IConfiguration configuration) : ILocationImageService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    
    public async Task UploadLocation(Stream stream, string id)
    {
        await GetClient().PutAsync(GetUrl(id), new StreamContent(stream));
    }

    public async Task RemoveLocation(string id)
    {
        await GetClient().DeleteAsync(GetUrl(id));
    }

    public string GetUrl(string id)
    {
        return new Uri(_pfpBucketUrl, $"locations/{id}").ToString();
    }
    
    private HttpClient GetClient()
    {
        return clientFactory.CreateClient("hetzner-storage");
    }
}