namespace Server.Services;

public interface IProfilePictureService
{
    public Task UploadProfilePictureAsync(Stream pictureStream, string email);
    public string GetDownloadUrl(string email);
    public Task RemoveProfilePictureAsync(string email);
}