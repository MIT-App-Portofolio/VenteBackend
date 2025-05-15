using Server.Data;

namespace Server.Services.Interfaces;

public interface IProfilePictureService
{
    public Task UploadProfilePictureAsync(Stream pictureStream, string username);
    public string GetDownloadUrl(ApplicationUser user);
    public string GetDownloadUrl(string username, int pfpVersion);
    public string GetFallbackUrl();

    public Task UploadReportPictureAsync(Stream pictureStream, string username, int pfpVersion);
    public Task DeleteReportPictureAsync(string username, int pfpVersion);
    public string GetReportUrl(string userName, int pfpVersion);

    public Task RemoveProfilePictureAsync(string username);
}