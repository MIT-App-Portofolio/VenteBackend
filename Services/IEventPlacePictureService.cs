using Server.Data;

namespace Server.Services;

public interface IEventPlacePictureService
{
    public List<string> GetDownloadUrls(EventPlace place);
}