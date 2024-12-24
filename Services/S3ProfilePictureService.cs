using Amazon.Runtime.SharedInterfaces;
using Microsoft.Extensions.Options;
using Server.Config;

namespace Server.Services;

public class S3ProfilePictureService(ICoreAmazonS3 s3Client, IOptions<AwsConfig> config) : IProfilePictureService
{
    private readonly string _bucketName = config.Value.MainBucketName;

    public async Task UploadProfilePictureAsync(Stream pictureStream, string email)
    {
        var path = "profile-pictures/" + email + ".jpeg";
        await s3Client.UploadObjectFromStreamAsync(_bucketName, path, pictureStream,
            new Dictionary<string, object>());
        await s3Client.MakeObjectPublicAsync(_bucketName, path, true);
    }
    
    public string GetDownloadUrl(string email)
    {
        return s3Client.GeneratePreSignedURL(_bucketName, "profile-pictures/" + email + ".jpeg", DateTime.Now.AddHours(1), new Dictionary<string, object>());
    }

    public async Task RemoveProfilePictureAsync(string email)
    {
        await s3Client.DeleteAsync(_bucketName, "profile-pictures/" + email + ".jpeg", new Dictionary<string, object>());
    }
}