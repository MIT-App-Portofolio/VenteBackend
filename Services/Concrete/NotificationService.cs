using FirebaseAdmin.Messaging;
using Server.Data;
using Message = FirebaseAdmin.Messaging.Message;
using Notification = FirebaseAdmin.Messaging.Notification;

namespace Server.Services.Concrete;

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
                Title = $"{invitor} te ha invitado a una escapada",
                Body = "Haz click para ver más detalles",
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "invite",
                ["link"] = "/calendar"
            },
            Token = target.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }

    public Task SendInviteAcceptedNotification(ApplicationUser target, string member)
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
                Title = $"{member} se ha unido a tu escapada.",
                Body = "Haz click para ver más detalles",
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "invite_accept",
                ["link"] = "/calendar"
            },
            Token = target.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }

    public Task SendDmNotification(ApplicationUser destination, string from, Data.Message dm)
    {
        if (destination.NotificationKey == null)
        {
            logger.LogWarning("User {0} has no notification key", destination.UserName);
            return Task.CompletedTask;
        }
        
        var message = new Message
        {
            Notification = new Notification
            {
                Title = $"{from} te ha enviado un mensaje.",
                Body = dm.MessageType switch
                {
                    MessageType.Text => dm.TextContent,
                    MessageType.Voice => "Mensaje de voz",
                    _ => throw new ArgumentOutOfRangeException()
                },
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "dm",
                ["link"] = "/messages?selectedUser="+from
            },
            Token = destination.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }

    public Task SendGroupDmNotifications(List<ApplicationUser> targets, int exitId, string groupName, string from,
        GroupMessage dm)
    {
        var messages = new List<Message>();

        foreach (var target in targets)
        {
            if (target.NotificationKey == null)
            {
                logger.LogWarning("User {0} has no notification key", target.UserName);
                continue;
            }

            messages.Add(new Message
            {
                Notification = new Notification
                {
                    Title = $"{groupName}: {from} ha enviado un mensaje.",
                    Body = dm.MessageType switch
                    {
                        MessageType.Text => dm.TextContent,
                        MessageType.Voice => "Mensaje de voz",
                        _ => throw new ArgumentOutOfRangeException()
                    },
                },
                Data = new Dictionary<string, string>
                {
                    ["notification_type"] = "dm_group",
                    ["link"] = "/messages?selectedExitId=" + exitId
                },
                Token = target.NotificationKey
            });
        }
        
        if (!messages.Any()) return Task.CompletedTask;

        return FirebaseMessaging.DefaultInstance.SendEachAsync(messages);
    }

    public Task SendLikeNotification(ApplicationUser destination, string liker)
    {
        if (destination.NotificationKey == null)
        {
            logger.LogWarning("User {0} has no notification key", destination.UserName);
            return Task.CompletedTask;
        }
        
        var message = new Message
        {
            Notification = new Notification
            {
                Title = $"{liker} te ha dado un like.",
                Body = "Haz click para ver mas detalles."
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "like",
                ["link"] = "/notifications"
            },
            Token = destination.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
    
    public Task SendFollowRequestNotification(ApplicationUser destination, string follower)
    {
        if (destination.NotificationKey == null)
        {
            logger.LogWarning("User {0} has no notification key", destination.UserName);
            return Task.CompletedTask;
        }
        
        var message = new Message
        {
            Notification = new Notification
            {
                Title = $"{follower} ha solicitado seguirte.",
                Body = "Haz click para rechazar/aceptar."
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "follow",
                ["link"] = "/social"
            },
            Token = destination.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }
    
    public Task SendFollowAcceptNotification(ApplicationUser destination, string follower)
    {
        if (destination.NotificationKey == null)
        {
            logger.LogWarning("User {0} has no notification key", destination.UserName);
            return Task.CompletedTask;
        }
        
        var message = new Message
        {
            Notification = new Notification
            {
                Title = $"{follower} ha aceptado tu solicitud.",
                Body = "Haz click para ver mas detalles."
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "follow_accept",
                ["link"] = "/social"
            },
            Token = destination.NotificationKey
        };
        
        return FirebaseMessaging.DefaultInstance.SendAsync(message);
    }

    public Task SendOfferNotification(List<string?> tokens, CustomOffer offer, EventPlace sender)
    {
        tokens = tokens.Where(t => t != null).ToList();
        
        var message = new MulticastMessage
        {
            Notification = new Notification
            {
                Title = $"{sender.Name} te ha ofrecido \"{offer.Name}\"",
                Body = "Haz click para ver más detalles",
            },
            Data = new Dictionary<string, string>
            {
                ["notification_type"] = "offer",
                ["link"] = "/offers"
            },
            Tokens = tokens
        };
        
        return FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
    }
}