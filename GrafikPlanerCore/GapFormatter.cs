namespace GrafikPlanerCore;

public static class GapFormatter
{
    private static readonly char[] SuperscriptDigits = { '⁰', '¹', '²', '³', '⁴', '⁵', '⁶', '⁷', '⁸', '⁹' };

    public static Dictionary<DateOnly, string> FormatGaps(List<DateTime> emptyHours)
    {
        return emptyHours
            .GroupBy(dt => DateOnly.FromDateTime(dt))
            .ToDictionary(
                g => g.Key,
                g => FormatRanges(g.Select(dt => dt.Hour).OrderBy(h => h).ToList())
            );
    }

    private static string FormatHour(int hour)
    {
        return $"{hour}{SuperscriptDigits[0]}{SuperscriptDigits[0]}";
    }

    private static string FormatRanges(List<int> hours)
    {
        if (hours.Count == 0) return "";

        var ranges = new List<string>();
        int start = hours[0], end = hours[0];

        for (int i = 1; i < hours.Count; i++)
        {
            if (hours[i] == end + 1)
            {
                end = hours[i];
            }
            else
            {
                ranges.Add($"{FormatHour(start)}-{FormatHour(end + 1)}");
                start = end = hours[i];
            }
        }
        ranges.Add($"{FormatHour(start)}-{FormatHour(end + 1)}");

        return string.Join("\n", ranges);
    }
}
