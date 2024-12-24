using Microsoft.AspNetCore.Identity;

namespace Server.Data;

public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public EventStatus EventStatus { get; set; }
}

public class EventStatus
{
    public int Id { get; set; }
    public bool Active { get; set; }
    public DateTime? Time { get; set; }
    public List<string>? With { get; set; }
    public Location? Location { get; set; }
}

public enum Location
{
    Salou
}
