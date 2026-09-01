using GrafikPlanerData.Models;
using GrafikPlanerData.Models.Enums;

namespace GrafikPlanerCore;

/// <summary>
/// Wylicza miesięczną normę godzinową pracownika na podstawie wymiaru etatu,
/// normy dobowej i liczby dni roboczych w danym miesiącu.
/// </summary>
public static class NormCalculator
{
    /// <summary>
    /// Oblicza normę godzinową pracownika na dany miesiąc.
    /// </summary>
    /// <param name="year">Rok</param>
    /// <param name="month">Miesiąc (1-12)</param>
    /// <param name="employee">Rekord pracownika</param>
    /// <param name="holidays">Lista aktywnych świąt</param>
    /// <returns>Norma godzinowa (decimal, np. 168.0)</returns>
    public static decimal Calculate(int year, int month, EmployeeRecord employee, List<HolidayRecord> holidays)
    {
        int workingDays = CountWorkingDays(year, month, holidays);
        decimal dailyNorm = 8m;
        decimal rate = employee.WorkTimeRate switch
        {
            WorkTimeRate.Full => 1.0m,
            WorkTimeRate.ThreeQuarters => 0.75m,
            WorkTimeRate.Half => 0.5m,
            WorkTimeRate.Quarter => 0.25m,
            _ => 1.0m
        };
        return workingDays * dailyNorm * rate;
    }

    /// <summary>
    /// Zwraca współczynnik etatu jako decimal.
    /// </summary>
    public static decimal GetRateMultiplier(WorkTimeRate rate)
    {
        return rate switch
        {
            WorkTimeRate.Full => 1.0m,
            WorkTimeRate.ThreeQuarters => 0.75m,
            WorkTimeRate.Half => 0.5m,
            WorkTimeRate.Quarter => 0.25m,
            _ => 1.0m
        };
    }

    /// <summary>
    /// Liczy dni robocze w miesiącu (pon-pt minus aktywne święta).
    /// </summary>
    private static int CountWorkingDays(int year, int month, List<HolidayRecord> holidays)
    {
        var holidayDates = ResolveHolidayDates(year, holidays)
            .Where(d => d.Month == month)
            .ToHashSet();

        int daysInMonth = DateTime.DaysInMonth(year, month);
        int workingDays = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var dayOfWeek = date.DayOfWeek;

            if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday &&
                !holidayDates.Contains(date))
            {
                workingDays++;
            }
        }

        return workingDays;
    }

    /// <summary>
    /// Rozwiązuje daty świąt (stałe i wielkanocne) na konkretne DateOnly dla danego roku.
    /// </summary>
    private static List<DateOnly> ResolveHolidayDates(int year, List<HolidayRecord> holidays)
    {
        var easter = PolishHolidays.ComputeEasterSunday(year);
        var dates = new List<DateOnly>();

        foreach (var h in holidays.Where(h => h.IsActive))
        {
            if (h.Month.HasValue && h.Day.HasValue)
            {
                dates.Add(new DateOnly(year, h.Month.Value, h.Day.Value));
            }
            else if (h.EasterOffset.HasValue)
            {
                dates.Add(easter.AddDays(h.EasterOffset.Value));
            }
        }

        return dates;
    }
}
