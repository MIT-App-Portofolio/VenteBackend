using System.ComponentModel.DataAnnotations;
using Server.Data;

namespace Server.Models;

public class EventPlaceModel {
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
    [Required]
    public List<string> Images { get; set; }
}