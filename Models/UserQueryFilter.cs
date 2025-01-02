using Server.Data;

namespace Server.Models;

public class UserQueryFilter
{
    public Gender? Gender { get; set; }
    public int? AgeRangeMin { get; set; }
    public int? AgeRangeMax { get; set; }
}