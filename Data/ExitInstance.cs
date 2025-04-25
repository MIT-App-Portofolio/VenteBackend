namespace Server.Data;

/// <summary>
/// An instance of a weekend exit of one or more people
/// </summary>
public class ExitInstance
{
    public int Id { get; set; }
    
    public int? AlbumId { get; set; }
    
    public required string Name { get; set; }
    
    public required string LocationId { get; set; }
    
    public required List<DateTimeOffset> Dates { get; set; }
    
    /// Username
    public required string Leader { get; set; }
    /// Usernames
    public required List<string> Members { get; set; }
    /// Usernames
    public required List<string> Invited { get; set; }
}