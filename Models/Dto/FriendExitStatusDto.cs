namespace Server.Models.Dto;

public class FriendExitStatusDto
{
    public string DisplayName { get; set; }
    public string PfpUrl { get; set; }
    public string LocationId { get; set; }
    public List<DateTime> Dates { get; set; }
}