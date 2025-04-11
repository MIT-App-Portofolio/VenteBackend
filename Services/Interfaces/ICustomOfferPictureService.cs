namespace Server.Services.Interfaces;

public interface ICustomOfferPictureService
{
    public string GetUrl(int offerId, string placeName);
    public Task UploadPicture(Stream stream, int offerId, string placeName);
    public Task DeletePicture(int offerId, string placeName);
}