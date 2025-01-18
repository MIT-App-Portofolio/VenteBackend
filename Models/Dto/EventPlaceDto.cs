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
    public int? AgeRequirement { get; set; } = place.AgeRequirement;
    public List<EventPlaceEventDto> Events { get; set; } = place.Events.Select(o => new EventPlaceEventDto(o)).ToList();
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