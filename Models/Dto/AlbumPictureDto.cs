using Server.Data;

namespace Server.Models.Dto;

public class AlbumPictureDto
{
    public AlbumPictureDto() { }

    public AlbumPictureDto(AlbumPicture picture)
    {
        Id = picture.Id;
        Uploader = picture.Uploader;
        Time = picture.Time;
    }
    
    public int Id { get; set; }
    public string Uploader { get; set; }
    public DateTimeOffset Time { get; set; }
}