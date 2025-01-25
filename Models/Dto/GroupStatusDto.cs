using Microsoft.AspNetCore.Identity;
using Server.Data;

namespace Server.Models.Dto;

/// <summary>
/// A class that shall be sent to members of the group but not other users as it contains "sensitive" info such as users awaiting for invite.
/// </summary>
public class GroupStatusDto
{
    public static async Task<GroupStatusDto> FromGroupAsync(EventGroup group, UserManager<ApplicationUser> userManager)
    {
        var members = new List<string>();
        var awaitingInvite = new List<string>();
        foreach (var userId in group.Members)
        {
            var user = await userManager.FindByIdAsync(userId);
            members.Add(user.UserName);
        }
        foreach (var userId in group.AwaitingInvite)
        {
            var user = await userManager.FindByIdAsync(userId);
            awaitingInvite.Add(user.UserName);
        }
        return new GroupStatusDto
        {
            Members = members,
            AwaitingInvite = awaitingInvite
        };
    }

    public List<string> Members { get; set; } = [];
    public List<string> AwaitingInvite { get; set; } = [];
}