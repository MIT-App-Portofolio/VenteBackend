using Server.Data;

namespace Server.Models.Dto;

public class CustomOfferDto
{
    public CustomOfferDto() { }

    public CustomOfferDto(CustomOffer offer, string? imageUrl, EventPlaceDto place)
    {
        Name = offer.Name;
        Place = place;
        Description = offer.Description;
        ImageUrl = offer.HasImage ? imageUrl : null;
        ValidUntil = offer.ValidUntil;
    }
    
    public EventPlaceDto Place { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}