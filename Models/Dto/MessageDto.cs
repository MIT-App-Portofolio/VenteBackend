using Server.Data;

namespace Server.Models.Dto;

public class MessageDto
{
    public MessageDto() { }
    
    public MessageDto(Message message, string dtoReceiver)
    {
        var outgoing = message.From == dtoReceiver;
        Id = message.Id;
        Type = outgoing ? "Outgoing" : "Incoming";
        MessageType = message.MessageType switch
        {
            Data.MessageType.Text => "Text",
            Data.MessageType.Voice => "Voice",
            _ => throw new ArgumentOutOfRangeException()
        };
        User = outgoing ? message.To : message.From;
        Read = message.Read;
        TextContent = message.TextContent;
        Timestamp = message.Timestamp;
    }
    
    public int Id { get; set; }
    
    public string User { get; set; }
    public bool Read { get; set; }

    /// Incoming/Outgoing
    public string Type { get; set; }
    /// Text/Voice
    public string MessageType { get; set; }
    public string? TextContent { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
