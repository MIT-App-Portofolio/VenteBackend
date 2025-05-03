using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class EventPlaceEventModel
{
    public EventPlaceEventModel() { }

    public EventPlaceEventModel(EventPlaceEvent eventPlaceEvent)
    {
        Name = eventPlaceEvent.Name;
        Time = eventPlaceEvent.Time.DateTime;
        Description = eventPlaceEvent.Description;
        PurchaseLink = eventPlaceEvent.PurchaseLink;
    }
    
    [Required]
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? PurchaseLink { get; set; }
    [Required]
    public DateTime Time { get; set; }
}