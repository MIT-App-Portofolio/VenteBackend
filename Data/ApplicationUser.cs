using Microsoft.AspNetCore.Identity;

namespace Server.Data;

public class ApplicationUser : IdentityUser
{
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public string? NotificationKey { get; set; }
    public DateTimeOffset BirthDate { get; set; }
    public bool HasPfp { get; set; }
    public int PfpVersion { get; set; }
    public Gender Gender { get; set; }
    public EventStatus EventStatus { get; set; }
    
    public List<string>? Blocked { get; set; }

    public EventPlace? EventPlace { get; set; }
}

public enum Gender
{
    Male,
    Female
}
