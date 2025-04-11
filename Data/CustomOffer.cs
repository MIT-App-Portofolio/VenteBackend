namespace Server.Data;

public class CustomOffer
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public int EventPlaceId { get; set; }
    public List<string> DestinedTo { get; set; }
    
    public bool HasImage { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}