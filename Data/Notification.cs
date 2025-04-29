namespace Server.Data;

public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; }
    public bool Read { get; set; }
    /// <summary>
    ///  This type isn't actually optional but for migration purposes it now is
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
    
    /// <summary>
    /// A username of a user that will appear once clicked on notifications
    /// </summary>
    public string? ReferenceUsername { get; set; }
}

public enum NotificationType
{
    Like
}