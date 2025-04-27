namespace Server.Data;

public class Message
{
    public int Id { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public bool Read { get; set; }
    public MessageType? MessageType { get; set; }
    public string? TextContent { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public enum MessageType
{
    Text,
    Voice
}