using System.Collections.Concurrent;
using System.Reflection;

namespace Server.Services.Concrete;

public class MessagingConnectionMap
{
    private readonly ConcurrentDictionary<string, string> _connections = new();

    public void Add(string username, string connectionId) =>
        _connections[username] = connectionId;

    public void Remove(string username) =>
        _connections.TryRemove(username, out _);

    public string? GetConnectionId(string username) =>
        _connections.TryGetValue(username, out var connectionId) ? connectionId : null;

    public (List<string> foundIds, List<string> missingUsernames) GetConnectionsIds(List<string> usernames)
    {
        var missing = new List<string>();
        var found = new List<string>();
        
        foreach (var username in usernames)
        {
            var id = GetConnectionId(username);
            if (id == null)
            {
                missing.Add(username);
            }
            else
            {
                found.Add(id);
            }
        }

        return (found, missing);
    }

    public int GetCount() =>
        _connections.Count;
}