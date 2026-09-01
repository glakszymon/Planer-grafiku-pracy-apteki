using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore;

public static class PolishHolidays
{
    public static List<(DateOnly Date, string Name)> GetForYear(int year)
    {
        var holidays = new List<(DateOnly, string)>
        {
            (new DateOnly(year, 1, 1), "Nowy Rok"),
            (new DateOnly(year, 1, 6), "Trzech Króli"),
            (new DateOnly(year, 5, 1), "Święto Pracy"),
            (new DateOnly(year, 5, 3), "Święto Konstytucji 3 Maja"),
            (new DateOnly(year, 8, 15), "Wniebowzięcie NMP"),
            (new DateOnly(year, 11, 1), "Wszystkich Świętych"),
            (new DateOnly(year, 11, 11), "Święto Niepodległości"),
            (new DateOnly(year, 12, 25), "Boże Narodzenie"),
            (new DateOnly(year, 12, 26), "Drugi dzień Bożego Narodzenia"),
        };

        var easter = ComputeEasterSunday(year);
        holidays.Add((easter.AddDays(1), "Poniedziałek Wielkanocny"));
        holidays.Add((easter.AddDays(60), "Boże Ciało"));

        holidays.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        return holidays;
    }

    /// <summary>
    /// Seeds Polish holidays for a given year into the database (idempotent).
    /// </summary>
    public static void SeedForYear(int year, HolidaysTable holidaysTable)
    {
        var holidays = GetForYear(year);
        foreach (var (date, name) in holidays)
        {
            if (!holidaysTable.ExistsBuiltInForDate(date))
            {
                holidaysTable.InsertHoliday(new HolidayRecord
                {
                    Date = date,
                    Name = name,
                    IsBuiltIn = true,
                    IsActive = true
                });
            }
        }
    }

    /// <summary>
    /// Anonymous Gregorian algorithm for computing Easter Sunday date.
    /// </summary>
    private static DateOnly ComputeEasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }
}
