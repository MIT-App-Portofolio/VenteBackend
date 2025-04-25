namespace Server;

public static class Utils
{
    public static int GetYears(this DateTimeOffset birthDate)
    {
        var years = DateTime.Now.Year - birthDate.Year;
        if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
            years--;
        return years;
    }
    
    public static bool DateRangeCheck(DateTimeOffset start1, DateTimeOffset? end1, DateTimeOffset targetStart,
        DateTimeOffset? targetEnd)
    {
        var range1Start = start1.Date;
        var range1End = (end1 ?? start1).Date;

        var range2Start = targetStart.Date.AddDays(-14);
        var range2End = (targetEnd ?? targetStart).Date.AddDays(14);

        return range1End >= range2Start && range1Start <= range2End;
    }
}