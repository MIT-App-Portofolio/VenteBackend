namespace Server.Models.Dto;

public class OwnPicturesDto
{
    public int AlbumId { get; set; }
    public List<AlbumPictureDto> Pictures { get; set; }
}