using Server.Data;

namespace Server.Models.Dto;

public class SharedAlbumDto
{
    public SharedAlbumDto() { }

    public SharedAlbumDto(SharedAlbum album)
    {
        Id = album.Id;
        LocationId = album.PlaceId;
        EventTime = album.EventDate;
        Pictures = album.Pictures.Select(p => new AlbumPictureDto(p)).ToList();
    }
    
    public int Id { get; set; }
    
    public string LocationId { get; set; }
    public DateTimeOffset EventTime { get; set; }
    
    public List<AlbumPictureDto> Pictures { get; set; }
}