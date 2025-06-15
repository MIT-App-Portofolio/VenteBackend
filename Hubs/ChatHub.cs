using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Concrete;
using MessageFeed = Server.Pages.Admin.MessageFeed;

namespace Server.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatHub(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, MessagingConnectionMap userConnections, NotificationService notificationService, ShadowedUsersTracker tracker, Services.Concrete.MessageFeed feed) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
        {
            Context.Abort();
            return;
        }
        userConnections.Add(username, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(username))
        {
            userConnections.Remove(username);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendDm(string username, string message, string tempId)
    {
        var senderUsername = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
        {
            Context.Abort();
            return;
        }

        var senderUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == senderUsername);

        if (senderUser == null)
        {
            Context.Abort();
            return;
        }

        if (senderUser.ShadowBanned)
        {
            tracker.AddAction(senderUsername, $"Sent dm to {username}: {message}");
            return;
        }

        var destination = await userManager.Users.Where(u => u.UserName == username).FirstOrDefaultAsync();

        if (destination == null) return;
        
        feed.RegisterMessage(senderUsername, destination.UserName, message);

        if (destination.Blocked != null && destination.Blocked.Contains(senderUsername))
            return;

        var dbMessage = new Message
        {
            From = senderUsername,
            To = username,
            Read = false,
            MessageType = MessageType.Text,
            TextContent = message,
            Timestamp = DateTimeOffset.UtcNow,
        };
        
        dbContext.Messages.Add(dbMessage);
        await dbContext.SaveChangesAsync();
        
        await Clients.Client(Context.ConnectionId).SendAsync("MessageAck", username, tempId, dbMessage.Id);

        var connectionId = userConnections.GetConnectionId(username);

        if (connectionId != null)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", new MessageDto(dbMessage, username));
        }
        else
        {
            await notificationService.SendDmNotification(destination, senderUsername, dbMessage);
        }
    }

    public async Task MarkRead(string partnerUsername)
    {
        var currentUsername = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(currentUsername))
        {
            Context.Abort();
            return;
        }

        var unreadMessageCount = await dbContext.Messages
            .Where(m => m.From == partnerUsername && m.To == currentUsername && !m.Read)
            .ExecuteUpdateAsync(m => m.SetProperty(x => x.Read, true));

        if (unreadMessageCount > 0)
        {
            var connectionId = userConnections.GetConnectionId(partnerUsername);
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("MessageRead", currentUsername);
            }
        }
    }
    public async Task SendGroupDm(int exitId, string message, string tempId)
    {
        var senderUsername = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        var senderUser = await userManager.Users.FirstOrDefaultAsync(u => u.UserName == senderUsername);

        if (senderUser == null)
        {
            Context.Abort();
            return;
        }
        
        var exit = await dbContext.Exits.FirstOrDefaultAsync(e =>
            e.Id == exitId && (e.Members.Contains(senderUsername) || e.Leader == senderUsername));
        
        if (exit == null) return;

        if (senderUser.ShadowBanned)
        {
            tracker.AddAction(senderUsername, $"Sent group dm to {exitId}: {message}");
            return;
        }
        
        var dbMessage = new GroupMessage
        {
            From = senderUsername,
            ExitId = exitId,
            MessageType = MessageType.Text,
            TextContent = message,
            ReadBy = [],
            Timestamp = DateTimeOffset.UtcNow,
        };
        
        dbContext.GroupMessages.Add(dbMessage);
        await dbContext.SaveChangesAsync();
        
        await Clients.Client(Context.ConnectionId).SendAsync("GroupMessageAck", exitId, tempId, dbMessage.Id);
        
        List<string> notifReceivers = [..exit.Members, exit.Leader];
        notifReceivers.Remove(senderUsername);

        var res = userConnections.GetConnectionsIds(notifReceivers);

        await Clients.Clients(res.foundIds).SendAsync("ReceiveGroupMessage", exitId, new GroupMessageDto(dbMessage));

        await notificationService.SendGroupDmNotifications(
            await userManager.Users.Where(u => res.missingUsernames.Contains(u.UserName)).ToListAsync(), exit.Id,
            exit.Name, senderUsername, dbMessage);
    }

    public async Task MarkGroupRead(int exitId)
    {
        var currentUsername = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(currentUsername))
        {
            Context.Abort();
            return;
        }
        
        var exit = await dbContext.Exits.FirstOrDefaultAsync(e =>
            e.Id == exitId && (e.Members.Contains(currentUsername) || e.Leader == currentUsername));

        if (exit == null) return;

        var unreadMessages = await dbContext.GroupMessages
            .Where(m => m.ExitId == exitId && !m.ReadBy.Contains(currentUsername))
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.ReadBy.Add(currentUsername);
        }

        dbContext.GroupMessages.UpdateRange(unreadMessages);
        await dbContext.SaveChangesAsync();

        if (unreadMessages.Count > 0)
        {
            List<string> notifReceivers = [..exit.Members, exit.Leader];
            notifReceivers.Remove(currentUsername);
            var ret  = userConnections.GetConnectionsIds(notifReceivers);
            await Clients.Clients(ret.foundIds).SendAsync("GroupMessageRead", exitId, currentUsername);
        }
    }
}