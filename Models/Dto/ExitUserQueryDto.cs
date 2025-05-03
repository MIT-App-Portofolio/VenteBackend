using Server.Data;

namespace Server.Models.Dto;

public class ExitUserQueryDto
{
    public string UserName { get; set; }
    public Gender Gender { get; set; }
    
    public List<DateTime> Dates { get; set; }
    public List<ExitUserFriendDto> With { get; set; }
    
    public string? Note { get; set; }
    public int? Years { get; set; }
    public string? Name { get; set; }
    public string? IgHandle { get; set; }
    public string? Description { get; set; }
    
    public int Likes { get; set; }
    public bool UserLiked { get; set; }
    public bool Verified { get; set; }
    
    public int ExitId { get; set; }
}

public class ExitUserFriendDto
{
    public string DisplayName { get; set; }
    public string PfpUrl { get; set; }
}