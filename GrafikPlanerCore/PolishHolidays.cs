using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore;

public static class PolishHolidays
{
    /// <summary>
    /// All 13 Polish public holidays as recurring definitions.
    /// </summary>
    public static List<HolidayRecord> GetBuiltInDefinitions()
    {
        return new List<HolidayRecord>
        {
            // Fixed holidays
            new() { Month = 1,  Day = 1,  Name = "Nowy Rok", IsBuiltIn = true, IsActive = true },
            new() { Month = 1,  Day = 6,  Name = "Święto Trzech Króli", IsBuiltIn = true, IsActive = true },
            new() { Month = 5,  Day = 1,  Name = "Święto Pracy", IsBuiltIn = true, IsActive = true },
            new() { Month = 5,  Day = 3,  Name = "Święto Konstytucji 3 Maja", IsBuiltIn = true, IsActive = true },
            new() { Month = 8,  Day = 15, Name = "Wniebowzięcie NMP", IsBuiltIn = true, IsActive = true },
            new() { Month = 11, Day = 1,  Name = "Wszystkich Świętych", IsBuiltIn = true, IsActive = true },
            new() { Month = 11, Day = 11, Name = "Święto Niepodległości", IsBuiltIn = true, IsActive = true },
            new() { Month = 12, Day = 25, Name = "Pierwszy dzień Bożego Narodzenia", IsBuiltIn = true, IsActive = true },
            new() { Month = 12, Day = 26, Name = "Drugi dzień Bożego Narodzenia", IsBuiltIn = true, IsActive = true },
            
            // Easter-based movable holidays
            new() { EasterOffset = 0,  Name = "Niedziela Wielkanocna", IsBuiltIn = true, IsActive = true },
            new() { EasterOffset = 1,  Name = "Poniedziałek Wielkanocny", IsBuiltIn = true, IsActive = true },
            new() { EasterOffset = 49, Name = "Zesłanie Ducha Świętego (Zielone Świątki)", IsBuiltIn = true, IsActive = true },
            new() { EasterOffset = 60, Name = "Boże Ciało", IsBuiltIn = true, IsActive = true },
        };
    }

    /// <summary>
    /// Seeds built-in Polish holidays into the database (idempotent).
    /// </summary>
    public static void SeedBuiltIn(HolidaysTable holidaysTable)
    {
        var definitions = GetBuiltInDefinitions();
        foreach (var def in definitions)
        {
            if (!holidaysTable.ExistsBuiltIn(def.Month, def.Day, def.EasterOffset))
            {
                holidaysTable.InsertHoliday(def);
            }
        }
    }

    /// <summary>
    /// Resolves a recurring HolidayRecord to a concrete date for the given year.
    /// Returns null if the holiday doesn't fall in any valid date.
    /// </summary>
    public static DateOnly? ResolveDate(HolidayRecord holiday, int year)
    {
        if (holiday.Month.HasValue && holiday.Day.HasValue)
        {
            return new DateOnly(year, holiday.Month.Value, holiday.Day.Value);
        }
        if (holiday.EasterOffset.HasValue)
        {
            var easter = ComputeEasterSunday(year);
            return easter.AddDays(holiday.EasterOffset.Value);
        }
        return null;
    }

    /// <summary>
    /// Resolves all active holidays to concrete dates for a given year and month.
    /// </summary>
    public static HashSet<DateOnly> GetActiveHolidayDatesForMonth(HolidaysTable holidaysTable, int year, int month)
    {
        var allHolidays = holidaysTable.GetAllActiveHolidays();
        var result = new HashSet<DateOnly>();
        
        foreach (var holiday in allHolidays)
        {
            var date = ResolveDate(holiday, year);
            if (date.HasValue && date.Value.Month == month)
            {
                result.Add(date.Value);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Anonymous Gregorian algorithm for computing Easter Sunday date.
    /// </summary>
    public static DateOnly ComputeEasterSunday(int year)
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
