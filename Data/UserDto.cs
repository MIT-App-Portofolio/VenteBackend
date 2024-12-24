namespace Server.Data;

public class UserDto {
    public UserDto(ApplicationUser user)
    {
        UserName = user.UserName;
        Name = user.Name;
        IgHandle = user.IgHandle;
        Description = user.Description;
        EventStatus = new EventStatusDto
        {
            Active = user.EventStatus.Active,
            Time = user.EventStatus.Time,
            With = user.EventStatus.With,
            Location = user.EventStatus.Location
        };
    }
    
    public string UserName { get; set; }
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public EventStatusDto EventStatus { get; set; }
}

public class EventStatusDto
{
    public bool Active { get; set; }
    public DateTime? Time { get; set; }
    public List<string>? With { get; set; }
    public Location? Location { get; set; }
}