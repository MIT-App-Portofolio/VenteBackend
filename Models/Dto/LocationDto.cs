using Server.Data;

namespace Server.Models.Dto;

public class LocationDto(Location location)
{
    public int Id { get; set; } = (int)location;
    public string Name { get; set; } = location.ToString();
}