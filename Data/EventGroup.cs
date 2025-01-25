namespace Server.Data;

public class EventGroup
{
    public int Id { get; set; }
    public List<string> Members { get; set; }
    public List<string> AwaitingInvite { get; set; }
}