using System.Collections.Concurrent;

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
}