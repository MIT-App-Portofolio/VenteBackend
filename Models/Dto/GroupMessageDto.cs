using Server.Data;

namespace Server.Models.Dto;

public class GroupMessageDto
{
    public GroupMessageDto() { }

    public GroupMessageDto(GroupMessage message)
    {
        SenderUsername = message.From;
        MessageType = message.MessageType switch
        {
            Data.MessageType.Text => "Text",
            Data.MessageType.Voice => "Voice",
            _ => throw new ArgumentOutOfRangeException()
        };
        ReadBy = message.ReadBy;
        TextContent = message.TextContent;
    }

    public string SenderUsername { get; set; }
    public string MessageType { get; set; }
    public string? TextContent { get; set; }
    public List<string> ReadBy { get; set; }
}