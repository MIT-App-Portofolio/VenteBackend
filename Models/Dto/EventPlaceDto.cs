using Server.Data;

namespace Server.Models.Dto;

public class EventPlaceDto
{
    public EventPlaceDto(EventPlace place, List<string> downloadUrls)
    {
        Name = place.Name;
        Description = place.Description;
        LocationId = place.LocationId;
        ImageUrls = downloadUrls;
        PriceRangeBegin = place.PriceRangeBegin;
        PriceRangeEnd = place.PriceRangeEnd;
        AgeRequirement = place.AgeRequirement;
        GoogleMapsLink = place.GoogleMapsLink;
        Events = place.Events.Select(o => new EventPlaceEventDto(o)).ToList();
        Type = place.Type switch
        {
            EventPlaceType.Disco => "Disco",
            EventPlaceType.Bar => "Bar",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public EventPlaceDto()
    {
        
    }

    public string Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string LocationId { get; set; }
    public List<string> ImageUrls { get; set; }
    public int PriceRangeBegin { get; set; }
    public int PriceRangeEnd { get; set; }
    public int? AgeRequirement { get; set; }
    public string? GoogleMapsLink { get; set; }
    public List<EventPlaceEventDto> Events { get; set; }
}

public class EventPlaceEventDto
{
    public EventPlaceEventDto() { }

    public EventPlaceEventDto(EventPlaceEvent @event)
    {
        Name = @event.Name;
        Description = @event.Description;
        Time = @event.Time.DateTime;
        Offers = @event.Offers.Select(o => new EventPlaceOfferDto(o)).ToList();
        Image = @event.Image;
    }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime Time { get; set; }
    public string? Image { get; set; }
    public List<EventPlaceOfferDto> Offers { get; set; }
}

public class EventPlaceOfferDto
{
    public EventPlaceOfferDto() { }

    public EventPlaceOfferDto(EventPlaceOffer offer)
    {
        Name = offer.Name;
        Description = offer.Description;
        Price = offer.Price;
    }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public int? Price { get; set; }
}