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
}