using Server.Data;

namespace Server.Models.Dto;

public class GroupMessageSummaryDto
{
    public GroupMessageSummaryDto() { }

    public GroupMessageSummaryDto(GroupMessage message, ExitInstance exit)
    {
        GroupName = exit.Name;
        Timestamp = message.Timestamp;
        ExitId = exit.Id;
        SenderUsername = message.From;
        MessageType = message.MessageType switch
        {
            Data.MessageType.Text => "Text",
            Data.MessageType.Voice => "Voice",
            _ => throw new ArgumentOutOfRangeException()
        };
        TextContent = message.TextContent;
    }

    public DateTimeOffset Timestamp { get; set; }
    public string GroupName { get; set; }
    public int ExitId { get; set; }
    public string SenderUsername { get; set; }
    public string MessageType { get; set; }
    public string? TextContent { get; set; }
}