using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Interfaces;

namespace Server.Services.Concrete;

public class InternalUserQuery
{
    public string UserName { get; set; }
    public Gender Gender { get; set; }
    
    public bool HasPfp { get; set; }
    public int PfpVersion { get; set; }
    
    public List<DateTime> Dates { get; set; }
    public List<ExitUserFriendDto> With { get; set; }
    
    public string? Note { get; set; }
    public int? Years { get; set; }
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    public bool Verified { get; set; }
    
    public List<string> Likes { get; set; }
    public List<ExitUserAttendingEventDto> Events { get; set; }
    
    public int ExitId { get; set; }
}

public class ExitFeeds(IServiceProvider serviceProvider)
{
    private readonly Dictionary<string, List<InternalUserQuery>> _cache = new();
    private readonly Queue<string> _queue = new();

    public List<ExitUserQueryDto> GetFeed(string location, List<DateTimeOffset> dates, int? ageRangeMin,
        int? ageRangeMax, Gender? gender, List<string>? blocked, string receiverUsername)
    {
        lock (_cache)
        {
            using var scope = serviceProvider.CreateScope();
            
            if (!_cache.TryGetValue(location, out var feed)) return [];

            var query = feed
                .Where(u => u.HasPfp)
                .Where(u => u.Dates.Any(d => dates.Any(d2 => Math.Abs((d - d2).Days) < 14)))
                .OrderBy(u => u.Dates.Min(d => Math.Abs((d - dates[0]).Days)))
                .AsEnumerable();
            
            if (blocked != null)
                query = query.Where(u => !blocked.Contains(u.UserName));

            if (gender.HasValue)
                query = query.Where(u => u.Gender == gender.Value);

            if (ageRangeMin.HasValue)
            {
                query = query.Where(u => u.Years.HasValue && u.Years >= ageRangeMin.Value);
            }

            if (ageRangeMax.HasValue)
            {
                query = query.Where(u => u.Years.HasValue && u.Years < ageRangeMax.Value);
            }

            var users = query.ToList();

            return users.Select(u =>
                new ExitUserQueryDto
                {
                    UserName = u.UserName,
                    Gender = u.Gender,
                    Dates = u.Dates,
                    With = u.With,
                    Note = u.Note,
                    Years = u.Years,
                    Name = u.Name,
                    IgHandle = u.IgHandle,
                    Description = u.Description,
                    Likes = u.Likes.Count,
                    Verified = u.Verified,
                    UserLiked = u.Likes.Contains(receiverUsername),
                    ExitId = u.ExitId,
                    AttendingEvents = u.Events
                }).ToList();
        }
    }

    public List<InternalUserQuery> GetFullFeed(string location)
    {
        lock (_cache)
        {
            if (!_cache.TryGetValue(location, out var feed)) return [];

            return feed
                .OrderBy(u => u.Dates.Min(d => Math.Abs((d - DateTimeOffset.UtcNow).Days)))
                .ToList();
        }
    }

    public void Enqueue(string location)
    {
        lock (_queue)
        {
            if (_queue.Contains(location)) return;
            _queue.Enqueue(location);
            if (!_cache.TryGetValue(location, out var value) || value.Count < 30)
            {
                Task.Run(() => Update(location));
            }
        }
    }

    public Task ExecuteQueue()
    {
        lock (_queue)
        {
            List<Task> tasks = [];
            while (_queue.Count > 0)
            {
                var loc = _queue.Dequeue();
                tasks.Add(Update(loc));
            }

            return Task.WhenAll(tasks);
        }
    }

    private async Task Update(string location)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExitFeeds>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var pfpService = scope.ServiceProvider.GetRequiredService<IProfilePictureService>();
        var eventPictureService = scope.ServiceProvider.GetRequiredService<IEventPlacePictureService>();

        logger.LogInformation("Caching feed for {0}", location);
        var exits = await dbContext.Exits.Where(e => e.LocationId == location)
            .Select(e => new
            {
                e.Id,
                e.Members,
                e.Dates,
                e.Leader,
                e.Likes,
                e.AttendingEvents
            })
            .ToListAsync();

        var entry = new List<InternalUserQuery>();
        var usernames = exits.SelectMany(e => (List<string>) [..e.Members, e.Leader]).Distinct().ToList();
        var users = await userManager.Users.Where(u => usernames.Contains(u.UserName))
            .ToDictionaryAsync(u => u.UserName);


        var attendingEvents= exits
            .SelectMany(e => e.AttendingEvents.Values)
            .SelectMany(l => l)
            .Distinct()
            .ToList();

        var allEvents = await dbContext.EventPlaceEvents
            .Include(e => e.EventPlace)
            .Where(e => attendingEvents.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id);

        void AddUser(string username, List<DateTime> dates, List<string> with, Dictionary<string, List<string>> exitLikes, List<int> attendingEvents, int exitId)
        {
            if (!users.TryGetValue(username, out var user))
            {
                logger.LogWarning("User {0} not found. Ignoring", username);
                return;
            }

            if (user.ShadowBanned)
                return;

            with = with.Where(u => users.ContainsKey(u)).ToList();

            var withDtos = with.Where(u => u != username).Select(u =>
            {
                var usr = users[u];
                return new ExitUserFriendDto
                {
                    DisplayName = string.IsNullOrEmpty(usr.Name) ? "@" + usr.UserName : usr.Name,
                    PfpUrl = usr.HasPfp ? pfpService.GetDownloadUrl(usr) : pfpService.GetFallbackUrl()
                };
            }).ToList();

            entry.Add(new InternalUserQuery
            {
                UserName = user.UserName,
                Gender = user.Gender,
                Dates = dates,
                With = withDtos,
                Note = user.CustomNote,
                Likes = exitLikes.TryGetValue(username, out var like) ? like : [],
                Years = user.BirthDate?.GetYears(),
                PfpVersion = user.PfpVersion,
                Name = user.Name,
                HasPfp = user.HasPfp,
                Verified = user.Verified,
                IgHandle = user.IgHandle,
                Description = user.Description,
                ExitId = exitId,
                Events = attendingEvents
                    .Where(k => allEvents.ContainsKey(k))
                    .Select<int, EventPlaceEvent>(k => allEvents[k])
                    .Select(e => new ExitUserAttendingEventDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        ImageUrl = eventPictureService.GetEventPictureUrl(e.EventPlace, e.EventPlace.Events.FindIndex(e1 => e1.Id == e.Id))
                    })
                    .ToList()
            });
        }

        foreach (var exit in exits)
        {
            var with = (List<string>) [..exit.Members, exit.Leader];
            var dates = exit.Dates.Select(d => d.Date).ToList();

            foreach (var m in exit.Members)
            {
                AddUser(m, dates, with, exit.Likes, exit.AttendingEvents.TryGetValue(m, out var e) ? e : [], exit.Id);
            }

            AddUser(exit.Leader, dates, with, exit.Likes, exit.AttendingEvents.TryGetValue(exit.Leader, out var e1) ? e1 : [], exit.Id);
        }

        lock (_cache)
        {
            _cache[location] = entry;
        }
        stopwatch.Stop();
        logger.LogInformation("Cache finish for {0} with {1} users. Took {2} ms", location, entry.Count, stopwatch.ElapsedMilliseconds);
    }

    public void UpdateLike(bool add, string location, string username, int exitId, string liker)
    {
        lock (_cache)
        {
            _cache[location].ForEach(u =>
            {
                if (u.UserName != username || u.ExitId != exitId) return;
                
                if (add)
                    u.Likes.Add(liker);
                else
                    u.Likes.Remove(liker);
            });
        }
    }

    public List<FriendExitStatusDto> GetFriendStatuses(List<string> friends, IProfilePictureService pfpService)
    {
        var ret = new List<FriendExitStatusDto>();
        lock (_cache)
        {
            foreach (var (loc, value) in _cache)
            {
                ret.AddRange(value.Where(u => friends.Contains(u.UserName)).Select(u => new FriendExitStatusDto()
                {
                    Username = u.UserName,
                    Name = u.Name,
                    PfpUrl = u.HasPfp ? pfpService.GetDownloadUrl(u.UserName, u.PfpVersion) : pfpService.GetFallbackUrl(),
                    Dates = u.Dates,
                    LocationId = loc
                }));
            }
        }

        return ret;
    }

    public Dictionary<int, int> GetAttendancesPerEvent(string location)
    {
        lock (_cache)
        {
            _cache.TryGetValue(location, out var users);

            var ret = new Dictionary<int, int>();

            foreach (var e in users.SelectMany(u => u.Events))
            {
                if (!ret.TryAdd(e.Id, 1))
                {
                    ret[e.Id] += 1;
                }
            }

            return ret;
        }
    }

    public List<ExitUserQueryDto> GetEventAttendingUsers(string receiverUsername, string location, int eventId)
    {
        lock (_cache)
        {
            _cache.TryGetValue(location, out var users);
            return users
                .Where(u => u.Events.Any(e => e.Id == eventId))
                .Select(u => new ExitUserQueryDto()
                {
                    UserName = u.UserName,
                    Gender = u.Gender,
                    Dates = u.Dates,
                    With = u.With,
                    Note = u.Note,
                    Years = u.Years,
                    Name = u.Name,
                    IgHandle = u.IgHandle,
                    Description = u.Description,
                    Likes = u.Likes.Count,
                    Verified = u.Verified,
                    UserLiked = u.Likes.Contains(receiverUsername),
                    ExitId = u.ExitId,
                    AttendingEvents = u.Events
                })
                .ToList();
        }
    }

    public void ClearPastDates()
    {
        var now = DateTimeOffset.UtcNow.AddDays(1);
        lock (_cache)
        {
            foreach (var location in _cache.Keys)
            {
                _cache[location] = _cache[location].Where(e => !e.Dates.All(d => d < now)).ToList();
            }
        }
    }
}