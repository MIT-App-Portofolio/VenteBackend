using Server.Data;

namespace Server.Models.Dto;

public class EventPlaceDto(EventPlace place, List<string> downloadUrls)
{
    public string Name { get; set; } = place.Name;
    public string Description { get; set; } = place.Description;
    public LocationDto Location { get; set; } = new(place.Location);
    public List<string> ImageUrls { get; set; } = downloadUrls;
    public int PriceRangeBegin { get; set; } = place.PriceRangeBegin;
    public int PriceRangeEnd { get; set; } = place.PriceRangeEnd;
}