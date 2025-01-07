using Amazon.Runtime.SharedInterfaces;
using Microsoft.Extensions.Options;
using Server.Config;

namespace Server.Services;

public class S3ProfilePictureService(ICoreAmazonS3 s3Client, IOptions<AwsConfig> config) : IProfilePictureService
{
    private readonly string _bucketName = config.Value.MainBucketName;

    public async Task UploadProfilePictureAsync(Stream pictureStream, string username)
    {
        var path = "profile-pictures/" + username + ".jpeg";
        await s3Client.UploadObjectFromStreamAsync(_bucketName, path, pictureStream,
            new Dictionary<string, object>());
        await s3Client.MakeObjectPublicAsync(_bucketName, path, true);
    }
    
    public string GetDownloadUrl(string username)
    {
        return s3Client.GeneratePreSignedURL(_bucketName, "profile-pictures/" + username + ".jpeg", DateTime.Now.AddHours(1), new Dictionary<string, object>());
    }

    public string GetFallbackUrl()
    {
        return s3Client.GeneratePreSignedURL(_bucketName, "profile-pictures/fallback.jpeg", DateTime.Now.AddHours(1), new Dictionary<string, object>());
    }

    public async Task RemoveProfilePictureAsync(string username)
    {
        await s3Client.DeleteAsync(_bucketName, "profile-pictures/" + username + ".jpeg", new Dictionary<string, object>());
    }
}