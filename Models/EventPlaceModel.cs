using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class EventPlaceModel {
    public EventPlaceModel() { }

    public EventPlaceModel(EventPlace eventPlace)
    {
        Name = eventPlace.Name;
        Description = eventPlace.Description;
        Location = eventPlace.Location;
        PriceRangeStart = eventPlace.PriceRangeBegin;
        PriceRangeEnd = eventPlace.PriceRangeEnd;
    }

    [Required]
    public string Name { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public Location Location { get; set; }
    [Required]
    public int PriceRangeStart { get; set; }
    [Required]
    public int PriceRangeEnd { get; set; }
}