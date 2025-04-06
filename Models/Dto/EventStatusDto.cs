namespace Server.Models.Dto;

public class EventStatusDto
{
    public bool Active { get; set; }
    public DateTimeOffset? Time { get; set; }
    public List<string>? With { get; set; }
    public string? LocationId { get; set; }
}