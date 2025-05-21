using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Concrete;

namespace Server.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatHub(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, MessagingConnectionMap userConnections, NotificationService notificationService) : Hub
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

        if (!(await userManager.Users.AnyAsync(u => u.UserName == senderUsername))) return;

        var destination = await userManager.Users.Where(u => u.UserName == username).FirstOrDefaultAsync();

        if (destination == null) return;

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
    }}