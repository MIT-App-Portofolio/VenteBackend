namespace Server.Data;

public class EventStatus
{
    public int Id { get; set; }
    public bool Active { get; set; }
    public DateTime? Time { get; set; }
    public List<string>? With { get; set; }
    public Location? Location { get; set; }
}