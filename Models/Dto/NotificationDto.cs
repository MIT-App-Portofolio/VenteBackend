using Server.Data;

namespace Server.Models.Dto;

public class NotificationDto
{
    public NotificationDto() { }

    public NotificationDto(Notification notification)
    {
        Type = notification.Type switch
        {
            Data.NotificationType.Like => "Notification",
            _ => throw new ArgumentOutOfRangeException()
        };

        Message = notification.Message;
        Read = notification.Read;
        ReferenceUsername = notification.ReferenceUsername;
    }
    
    public string Type { get; set; }
    public string Message { get; set; }
    public bool Read { get; set; }
    
    /// <summary>
    /// A username of a user that will appear once clicked on notifications
    /// </summary>
    public string? ReferenceUsername { get; set; }
}
