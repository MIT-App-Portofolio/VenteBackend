using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class EventPlaceModel
{
    public EventPlaceModel() { }

    public EventPlaceModel(EventPlace eventPlace)
    {
        Name = eventPlace.Name;
        Description = eventPlace.Description;
        LocationId = eventPlace.LocationId;
        PriceRangeStart = eventPlace.PriceRangeBegin;
        PriceRangeEnd = eventPlace.PriceRangeEnd;
        AgeRequirement = eventPlace.AgeRequirement;
        GoogleMapsLink = eventPlace.GoogleMapsLink;
    }

    [Required]
    public string Name { get; set; }
    [Required]
    public string Description { get; set; }
    public string LocationId { get; set; }
    [Required]
    public int PriceRangeStart { get; set; }
    [Required]
    public int PriceRangeEnd { get; set; }
    public int? AgeRequirement { get; set; }
    public string? GoogleMapsLink { get; set; }
}