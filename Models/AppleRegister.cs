using Server.Data;

namespace Server.Models;

public class AppleRegister
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}