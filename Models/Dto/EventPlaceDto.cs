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
    public List<EventPlaceOfferDto> Offers { get; set; } = place.Offers.Select(o => new EventPlaceOfferDto(o)).ToList();
    public int? AgeRequirement { get; set; } = place.AgeRequirement;
}

public class EventPlaceOfferDto
{
    public EventPlaceOfferDto() { }

    public EventPlaceOfferDto(EventPlaceOffer offer)
    {
        ActiveOn = offer.ActiveOn;
        Name = offer.Name;
        Description = offer.Description;
        Image = offer.Image;
        Price = offer.Price;
    }
    
    public DateTime ActiveOn { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int? Price { get; set; }
}