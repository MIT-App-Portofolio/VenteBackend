using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Dto;
using Server.Services.Interfaces;

namespace Server.Services.Concrete;

public class ExitFeeds(IServiceProvider serviceProvider)
{
    private readonly Dictionary<string, List<ExitUserQueryDto>> _cache = new();
    private readonly Queue<string> _queue = new();

    public List<ExitUserQueryDto> GetFeed(string location, List<DateTimeOffset> dates, int? ageRangeMin,
        int? ageRangeMax, Gender? gender, List<string>? blocked)
    {
        lock (_cache)
        {
            if (!_cache.TryGetValue(location, out var feed)) return [];

            var query = feed
                .OrderBy(u => u.Dates.Min(d => Math.Abs((d - dates[0]).Days)))
                .Where(u => u.Dates.Any(d => dates.Any(d2 => Math.Abs((d - d2).Days) < 14)));
            
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
            
            return query.ToList();
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
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExitFeeds>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var pfpService = scope.ServiceProvider.GetRequiredService<IProfilePictureService>();

        logger.LogInformation("Caching feed for {0}", location);
        var exits = await dbContext.Exits.Where(e => e.LocationId == location)
            .Select(e => new
            {
                e.Members,
                e.Dates,
                e.Leader,
            })
            .ToListAsync();

        var entry = new List<ExitUserQueryDto>();
        var usernames = exits.SelectMany(e => (List<string>) [..e.Members, e.Leader]).Distinct().ToList();
        var users = await userManager.Users.Where(u => usernames.Contains(u.UserName))
            .ToDictionaryAsync(u => u.UserName);

        void AddUser(string username, List<DateTime> dates, List<string> with)
        {
            if (entry.Any(u => u.UserName == username)) return;

            var withDtos = with.Where(u => u != username).Select(u =>
            {
                var usr = users[u];
                return new ExitUserFriendDto
                {
                    DisplayName = usr.Name ?? "@" + usr.UserName,
                    PfpUrl = usr.HasPfp ? pfpService.GetDownloadUrl(usr.UserName) : pfpService.GetFallbackUrl()
                };
            }).ToList();

            var user = users[username];

            entry.Add(new ExitUserQueryDto
            {
                UserName = user.UserName,
                Gender = user.Gender,
                Dates = dates,
                With = withDtos,
                Note = user.CustomNote,
                Years = user.BirthDate?.GetYears(),
                Name = user.Name,
                IgHandle = user.IgHandle,
                Description = user.Description
            });
        }

        foreach (var exit in exits)
        {
            var with = (List<string>) [..exit.Members, exit.Leader];
            var dates = exit.Dates.Select(d => d.Date).ToList();

            foreach (var m in exit.Members)
            {
                AddUser(m, dates, with);
            }

            AddUser(exit.Leader, dates, with);
        }

        logger.LogInformation("Cache finish for {0} with {1} users", location, entry.Count);
        lock (_cache)
        {
            _cache[location] = entry;
        }
    }

    public void ClearPastDates()
    {
        var now = DateTimeOffset.UtcNow.AddDays(1);
        lock (_cache)
        {
            foreach (var location in _cache.Keys)
            {
                _cache[location] = _cache[location].Where(e => e.Dates.All(d => d < now)).ToList();
            }
        }
    }
}