namespace Server.Data;

public class EventStatus
{
    public int Id { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset? Time { get; set; }
    public string? LocationId { get; set; }
    
    public int? EventGroupInvitationId { get; set; }
    
    public int? AssociatedExitId { get; set; }

    public int? EventGroupId { get; set; }
}