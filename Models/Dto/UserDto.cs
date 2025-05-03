using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Models.Dto;

public class UserDto
{
    public UserDto()
    {
        
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="groupUserNames">It is safe to pass the db version of the usernames because the user itself will be removed from it</param>
    public UserDto(ApplicationUser user, List<string>? groupUserNames)
    {
        UserName = user.UserName!;
        Gender = user.Gender;
        Name = user.Name;
        Note = user.CustomNote;
        IgHandle = user.IgHandle;
        Description = user.Description;
        Verified = user.Verified;
        if (user.BirthDate.HasValue)
        {
            Years = DateTime.Now.Year - user.BirthDate.Value.Year;
            if (DateTime.Now.DayOfYear < user.BirthDate.Value.DayOfYear)
                Years--;
        }

        groupUserNames?.Remove(UserName);
        
        EventStatus = new EventStatusDto
        {
            Active = user.EventStatus.Active,
            Time = user.EventStatus.Time,
            With = groupUserNames,
            LocationId = user.EventStatus.LocationId
        };
    }

    public static async Task<List<UserDto>> FromListAsync(List<ApplicationUser> users, ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        var ret = new List<UserDto>();

        Dictionary<int, EventGroup> groups = new();
        foreach (var u in users)
        {
            List<string>? withUsernames = null;
            if (u.EventStatus.EventGroupId != null)
            {
                EventGroup? group;
                if (!groups.TryGetValue(u.EventStatus.EventGroupId.Value, out var group1))
                {
                    group = await dbContext.Groups.FindAsync(u.EventStatus.EventGroupId);

                    if (group == null) throw new UnreachableException("Group could not be found.");

                    groups.Add(u.EventStatus.EventGroupId.Value, group);
                }
                else
                {
                    group = group1;
                }

                withUsernames = await userManager.Users
                    .Where(u => group.Members.Contains(u.Id))
                    .Select(u => u.UserName!)
                    .ToListAsync();
            }

            ret.Add(new UserDto(u, withUsernames));
        }

        return ret;
    }

    public string UserName { get; set; }
    public Gender Gender { get; set; }
    public string? Note { get; set; }
    public int? Years { get; set; }
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public bool Verified { get; set; }
    public EventStatusDto EventStatus { get; set; }
}