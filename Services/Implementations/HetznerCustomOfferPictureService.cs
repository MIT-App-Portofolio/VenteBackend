using Server.Data;
using Server.Services.Interfaces;

namespace Server.Services.Implementations;

public class HetznerCustomOfferPictureService(IConfiguration configuration, IHttpClientFactory factory) : ICustomOfferPictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:PfpBucketUrl"]!);
    
    public string GetUrl(int offerId, string placeName)
    {
        return new Uri(_pfpBucketUrl, $"/custom_offers/{placeName}/{offerId}").ToString();
    }

    public Task UploadPicture(Stream stream, int offerId, string placeName)
    {
        return GetClient().PutAsync(GetUrl(offerId, placeName), new StreamContent(stream));
    }

    public Task DeletePicture(int offerId, string placeName)
    {
        return GetClient().DeleteAsync(GetUrl(offerId, placeName));
    }
    
    private HttpClient GetClient()
    {
        return factory.CreateClient("hetzner-storage");
    }
}