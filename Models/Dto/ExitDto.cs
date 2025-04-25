namespace Server.Models.Dto;

public class ExitDto
{
    public required int Id { get; set; }
    public required string LocationId { get; set; }
    public required string Name { get; set; }
    public required string Leader { get; set; }
    public required List<DateTime> Dates { get; set; }
    public required List<string> Members { get; set; }
    public required List<string> AwaitingInvite { get; set; }
}