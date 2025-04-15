namespace Server.Data;

public class SharedAlbum
{
    public int Id { get; set; }
    public string PlaceId { get; set; }
    public DateTimeOffset EventDate { get; set; }
    // When the album is available to be viewed in its entirety for its members
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset DeletionDate { get; set; }
    // Ids of members
    public List<string> Members { get; set; }
    public DateTimeOffset Created { get; set; }
    public List<AlbumPicture> Pictures { get; set; }
}

public class AlbumPicture
{
    public int Id { get; set; }
    // Username of uploader
    public string Uploader { get; set; }
    public DateTimeOffset Time { get; set; }
}