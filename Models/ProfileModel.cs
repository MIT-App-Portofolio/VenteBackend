using Server.Data;

namespace Server.Models;

public class ProfileModel
{
    public Gender Gender { get; set; }
    public string IgHandle { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}