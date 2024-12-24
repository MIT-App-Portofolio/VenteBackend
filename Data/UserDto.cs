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
        };
        
        if (user.EventStatus.Location != null)
            EventStatus.Location = new LocationDto(user.EventStatus.Location.Value);
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
    public LocationDto? Location { get; set; }
}

public class LocationDto
{
    public LocationDto(Location location)
    {
        Id = (int)location;
        Name = location.ToString();
    }
    
    public int Id { get; set; }
    public string Name { get; set; }
}
