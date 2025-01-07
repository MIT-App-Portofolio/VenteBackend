namespace Server.Services;

public interface IProfilePictureService
{
    public Task UploadProfilePictureAsync(Stream pictureStream, string username);
    public string GetDownloadUrl(string username);
    public string GetFallbackUrl();

    public Task RemoveProfilePictureAsync(string username);
}