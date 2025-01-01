namespace Server.Data;

public class EventPlace
{
    public int Id { get; set; }
    public ApplicationUser Owner { get; set; }
    public string OwnerId { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public Location Location { get; set; }
    public List<string> Images { get; set; }
    public int PriceRangeBegin { get; set; }
    public int PriceRangeEnd { get; set; }
    
    public List<EventPlaceOffer> Offers { get; set; }
}

public class EventPlaceOffer
{
    public int Id { get; set; }
    public EventPlace EventPlace { get; set; }
    public int EventPlaceId { get; set; }
    
    public DateTime ActiveOn { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public int? Price { get; set; }
}