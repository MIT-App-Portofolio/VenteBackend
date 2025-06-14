namespace Server.Services.Concrete;

public class MessageFeed
{
    private readonly Dictionary<(string,string), List<string>> _messages = [];
    public void RegisterMessage(string s, string r, string m)
    {
        var pair = (s, r);
        if (s.CompareTo(r) < 0)
        {
            pair = (r, s);
        }

        var entry = $"{s}: {m}";
        lock (_messages)
        {
            var nl = new List<string> { entry };

            if (!_messages.TryAdd(pair, nl))
            {
                _messages[pair].Add(entry);
            }
        }
    }

    public List<(string, string)> GetMessages()
    {
        lock (_messages)
        {
            return _messages
                .OrderByDescending(kv => kv.Value.Count)
                .Select(kv => ($"{kv.Key.Item1} and {kv.Key.Item2}", string.Join("\n", kv.Value)))
                .ToList();
        }
    }
}