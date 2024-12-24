using Server.Services;

namespace Server.Data;

public class EventPlace
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Location Location { get; set; }
    public List<string> Images { get; set; }
    public int PriceRangeBegin { get; set; }
    public int PriceRangeEnd { get; set; }
}

public class EventPlaceDto
{
    public EventPlaceDto(EventPlace place, List<string> downloadUrls)
    {
        Name = place.Name;
        Location = new LocationDto(place.Location);
        ImageUrls = downloadUrls;
        PriceRangeBegin = place.PriceRangeBegin;
        PriceRangeEnd = place.PriceRangeEnd;
    }
    
    public string Name { get; set; }
    public LocationDto Location { get; set; }
    public List<string> ImageUrls { get; set; }
    public int PriceRangeBegin { get; set; }
    public int PriceRangeEnd { get; set; }
}