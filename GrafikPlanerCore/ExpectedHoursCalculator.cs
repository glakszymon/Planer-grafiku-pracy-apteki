using GrafikPlanerData.Models;
using GrafikPlanerData.Models.Enums;

namespace GrafikPlanerCore;

/// <summary>
/// Oblicza oczekiwaną liczbę godzin pracy w miesiącu wg wzoru:
/// ((Dni pon-pt * 8) - (Święta pon-sob * 8)) * wymiar etatu.
/// Uwaga: święta przypadające w sobotę również pomniejszają wynik,
/// mimo że sobota nie jest dniem roboczym.
/// </summary>
public static class ExpectedHoursCalculator
{
    public static int Calculate(int year, int month, WorkTimeRate rate, List<HolidayRecord> holidays)
    {
        int monFriDays = CountMonFriDays(year, month);
        int monSatHolidays = CountMonSatHolidays(year, month, holidays);
        decimal rateMultiplier = NormCalculator.GetRateMultiplier(rate);
        return (int)((monFriDays * 8 - monSatHolidays * 8) * rateMultiplier);
    }

    private static int CountMonFriDays(int year, int month)
    {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int count = 0;
        for (int day = 1; day <= daysInMonth; day++)
        {
            var dow = new DateOnly(year, month, day).DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static int CountMonSatHolidays(int year, int month, List<HolidayRecord> holidays)
    {
        int count = 0;
        foreach (var holiday in holidays.Where(h => h.IsActive))
        {
            var date = PolishHolidays.ResolveDate(holiday, year);
            if (date.HasValue && date.Value.Month == month && date.Value.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }
}
