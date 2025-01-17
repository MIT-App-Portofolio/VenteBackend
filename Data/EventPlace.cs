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
    public int? AgeRequirement { get; set; }
    
    public List<EventPlaceEvent> Events { get; set; }
}

public class EventPlaceEvent
{
    public int Id { get; set; }
    public EventPlace EventPlace { get; set; }
    public int EventPlaceId { get; set; }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public DateTimeOffset Time { get; set; }
    
    public List<EventPlaceOffer> Offers { get; set; }
}

public class EventPlaceOffer
{
    public int Id { get; set; }
    public EventPlaceEvent Event { get; set; }
    public int EventId { get; set; }
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public int? Price { get; set; }
}