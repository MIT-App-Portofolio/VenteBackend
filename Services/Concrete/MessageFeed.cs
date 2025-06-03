namespace Server.Services.Concrete;

public class MessageFeed
{
    private readonly List<string> _messages = [];
    public void RegisterMessage(string s, string r, string m)
    {
        lock (_messages)
        {
            _messages.Add($"{s}->{r}: {m}");
        }
    }

    public string GetMessages()
    {
        return string.Join("\n", _messages);
    }
}