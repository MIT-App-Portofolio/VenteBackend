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
    public string? Description { get; set; }
    // has to be nullable bc reused for admin page and affiliate page, affiliates cannot change location id so the field shouldn't fail
    public string? LocationId { get; set; }
    public int? PriceRangeStart { get; set; }
    public int? PriceRangeEnd { get; set; }
    public int? AgeRequirement { get; set; }
    public string? GoogleMapsLink { get; set; }
}