using Amazon.Runtime.SharedInterfaces;
using Microsoft.Extensions.Options;
using Server.Config;
using Server.Data;

namespace Server.Services;

public class S3EventPlacePictureService(ICoreAmazonS3 _s3Client, IOptions<AwsConfig> config) : IEventPlacePictureService
{
    private readonly string _bucketName = config.Value.MainBucketName;
    
    public List<string> GetDownloadUrls(EventPlace place)
    {
        return place.Images.Select(image => _s3Client.GeneratePreSignedURL(_bucketName, "places-pictures/" + image, DateTime.Now.AddHours(1), new Dictionary<string, object>())).ToList();
    }
}