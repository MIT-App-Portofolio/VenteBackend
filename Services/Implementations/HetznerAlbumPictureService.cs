using Amazon.S3;
using Amazon.S3.Model;
using Server.Services.Interfaces;

namespace Server.Services.Implementations;

public class HetznerAlbumPictureService(IHttpClientFactory clientFactory, IConfiguration configuration)
    : IAlbumPictureService
{
    private readonly Uri _pfpBucketUrl = new(configuration["Hetzner:AlbumBucketUrl"]!);

    public Task UploadAlbumPicture(Stream stream, int albumId, int pictureId)
    {
        return GetClient().PutAsync(GetUrl(albumId, pictureId), new StreamContent(stream));
    }

    public Task RemoveAlbumPicture(int albumId, int pictureId)
    {
        return GetClient().DeleteAsync(GetUrl(albumId, pictureId));
    }

    public Task<Stream> GetStream(int albumId, int pictureId)
    {
        return GetClient().GetStreamAsync(GetUrl(albumId, pictureId));
    }

    private string GetUrl(int albumId, int pictureId)
    {
        return new Uri(_pfpBucketUrl, $"{albumId}/{pictureId}").ToString();
    }
    
    private HttpClient GetClient()
    {
        return clientFactory.CreateClient("hetzner-storage");
    }
}
