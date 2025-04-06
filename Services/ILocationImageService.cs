namespace Server.Services;

public interface ILocationImageService
{
    public Task UploadLocation(Stream stream, string id);
    public Task RemoveLocation(string id);
    public string GetUrl(string id);
}