using Server.Data;

namespace Server.Models.Dto;

public class LocationDto(LocationInfo location, string pictureUrl)
{
    public string Id { get; set; } = location.Id;
    public string Name { get; set; } = location.Name;

    public string PictureUrl { get; set; } = pictureUrl;
    
    public double Latitude { get; set; } = location.Latitude;
    public double Longitude { get; set; } = location.Longitude;
}