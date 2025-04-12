using Server.Data;

namespace Server.Models.Dto;

public class CustomOfferOnlyDto
{
    public CustomOfferOnlyDto() { }

    public CustomOfferOnlyDto(CustomOffer offer, string? imageUrl)
    {
        Name = offer.Name;
        Description = offer.Description;
        ImageUrl = offer.HasImage ? imageUrl : null;
        ValidUntil = offer.ValidUntil;
    }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}
