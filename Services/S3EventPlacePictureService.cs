using Amazon.Runtime.SharedInterfaces;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Data;

namespace Server.Services;

public class S3EventPlacePictureService(ICoreAmazonS3 s3Client, IOptions<AwsConfig> config) : IEventPlacePictureService
{
    private readonly string _bucketName = config.Value.MainBucketName;
    
    public List<string> GetDownloadUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                s3Client.GeneratePreSignedURL(_bucketName, "places-pictures/" + place.Name + "/" + image, 
                    DateTime.Now.AddHours(1), new Dictionary<string, object>()))
            .ToList();
    }

    public List<(string, string)> GetDownloadWithFilenameUrls(EventPlace place)
    {
        return place.Images
            .Select(image => 
                (image, s3Client.GeneratePreSignedURL(_bucketName, "places-pictures/" + place.Name + "/" + image, 
                    DateTime.Now.AddHours(1), new Dictionary<string, object>())))
            .ToList();
    }
    
    public string GetEventOfferPictureUrl(EventPlace place, int offerId)
    {
        var offer = place.Offers[offerId];
        var path = "places-pictures/" + place.Name + "/" + offer.Name + "/" + offer.Image;
        
        return s3Client.GeneratePreSignedURL(_bucketName, path, DateTime.Now.AddHours(1), 
            new Dictionary<string, object>());
    }
    
    public async Task UploadAsync(EventPlace place, Stream pictureStream, string filename)
    {
        var path = "places-pictures/" + place.Name + "/" + filename;
        await s3Client.UploadObjectFromStreamAsync(_bucketName, path, pictureStream,
            new Dictionary<string, object>());
        await s3Client.MakeObjectPublicAsync(_bucketName, path, true);
    }
    
    public async Task UploadEventOfferPictureAsync(EventPlace place, int offerId, Stream pictureStream, string filename)
    {
        var offer = place.Offers[offerId];
        var path = "places-pictures/" + place.Name + "/" + offer.Name + "/" + filename;
        await s3Client.UploadObjectFromStreamAsync(_bucketName, path, pictureStream,
            new Dictionary<string, object>());
        await s3Client.MakeObjectPublicAsync(_bucketName, path, true);
    }
    
    public async Task DeleteAsync(EventPlace place, string filename)
    {
        await s3Client.DeleteAsync(_bucketName, "places-pictures/" + place.Name + "/" + filename, new Dictionary<string, object>());
    }
}