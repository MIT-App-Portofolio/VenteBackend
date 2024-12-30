namespace Server.Models.Dto;

public class EventStatusDto
{
    public bool Active { get; set; }
    public DateTime? Time { get; set; }
    public List<string>? With { get; set; }
    public LocationDto? Location { get; set; }
}