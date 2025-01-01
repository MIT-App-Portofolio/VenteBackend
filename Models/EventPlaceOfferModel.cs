using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class EventPlaceOfferModel
{
    public EventPlaceOfferModel() { }

    public EventPlaceOfferModel(EventPlaceOffer offer)
    {
        Name = offer.Name;
        Price = offer.Price;
        ActiveOn = offer.ActiveOn;
        Description = offer.Description;
    }
    
    [Required]
    public string Name { get; set; }
    [Required]
    public DateTime ActiveOn { get; set; }
    
    public int? Price { get; set; }
    public string? Description { get; set; }
}