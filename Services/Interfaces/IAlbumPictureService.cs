namespace Server.Services.Interfaces;

public interface IAlbumPictureService
{
    public Task UploadAlbumPicture(Stream stream, int albumId, int pictureId);
    public Task RemoveAlbumPicture(int albumId, int pictureId);

    public Task<Stream> GetStream(int albumId, int pictureId);
}