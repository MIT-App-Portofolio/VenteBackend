namespace Server.Data;

public class EventGroup
{
    public int Id { get; set; }
    public int? SharedAlbumId { get; set; }
    // Ids of members
    public List<string> Members { get; set; }
    // Ids of users awaiting to be invited
    public List<string> AwaitingInvite { get; set; }
}