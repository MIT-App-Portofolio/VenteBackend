using Server.Data;

namespace Server.Services;

public interface IEventPlacePictureService
{
    public List<string> GetDownloadUrls(EventPlace place);
    public List<(string, string)> GetDownloadWithFilenameUrls(EventPlace place);
    public string GetEventPictureUrl(EventPlace place, int offerId);
    
    public Task UploadAsync(EventPlace place, Stream picture, string filename);
    public Task UploadEventPictureAsync(EventPlace place, int offerId, Stream picture, string filename);
    
    public Task DeleteAsync(EventPlace place, string filename);
    public Task DeleteEventPictureAsync(EventPlace place, int offerId);
}