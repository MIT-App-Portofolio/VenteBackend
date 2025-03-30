namespace Server.Data;

public class Report
{
    public int Id { get; set; }
    
    public int ReportCount { get; set; }
    
    public string Username { get; set; }
    
    public bool HasPfp { get; set; }
    
    public string? Name { get; set; }
    public string? Description { get; set;}
    public string? IgHandle { get; set;}
    public Gender Gender { get; set; }
    public int PfpVersion { get; set; }
}