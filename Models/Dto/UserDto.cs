using Server.Data;

namespace Server.Models.Dto;

public class UserDto
{
    public UserDto(ApplicationUser user)
    {
        UserName = user.UserName!;
        Gender = user.Gender;
        Name = user.Name;
        IgHandle = user.IgHandle;
        Description = user.Description;
        Years = DateTime.Now.Year - user.BirthDate.Year;
        if (DateTime.Now.DayOfYear < user.BirthDate.DayOfYear)
            Years--;
        
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
    public Gender Gender { get; set; }
    public int Years { get; set; }
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public EventStatusDto EventStatus { get; set; }
}