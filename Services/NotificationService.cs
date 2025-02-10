using FirebaseAdmin.Messaging;
using Server.Data;

namespace Server.Services;

public class NotificationService(ILogger<NotificationService> logger)
{
    public Task SendInviteNotification(ApplicationUser target, string invitor)
    {
        if (target.NotificationKey == null)
        {
            logger.LogWarning("User {0} has no notification key", target.UserName);
            return Task.CompletedTask;
        }
        
        var message = new Message
        {
            Notification = new Notification
            {
                Title = $"{invitor} te ha invitado a un evento",
                Body = "Haz click para ver más detalles"
            },
            Token = target.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
}