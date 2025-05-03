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

    public static string GetDateListDisplay(this List<DateTime> dates)
    {
        var orderedDates = dates.Select(d => d.Date).OrderBy(d => d).ToList();

        var ranges = new List<string>();
        var rangeStart = orderedDates[0];
        var rangeEnd = orderedDates[0];

        for (int i = 1; i <= orderedDates.Count; i++)
        {
            DateTime? currentDate = i < orderedDates.Count ? orderedDates[i] : null;
            DateTime prevDate = orderedDates[i - 1];

            if (!currentDate.HasValue || (currentDate.Value - prevDate).TotalDays > 1)
            {
                if (rangeStart == rangeEnd)
                {
                    ranges.Add(DateShortDisplay(rangeStart));
                }
                else
                {
                    ranges.Add($"{DateShortDisplay(rangeStart)}-{DateShortDisplay(rangeEnd)}");
                }

                if (currentDate.HasValue)
                {
                    rangeStart = currentDate.Value;
                    rangeEnd = currentDate.Value;
                }
            }
            else
            {
                rangeEnd = currentDate.Value;
            }
        }

        return string.Join(", ", ranges);
    }

    public static string DateShortDisplay(this DateTime date)
    {
        return date.ToString("dd/MM");
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