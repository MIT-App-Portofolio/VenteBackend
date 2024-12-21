namespace Server.Config;

public class AwsConfig
{
    public string AccessKeyId { get; set; }
    public string SecretAccessKey { get; set; }
    public string Region { get; set; }
    public string PfpBucketName { get; set; }
}