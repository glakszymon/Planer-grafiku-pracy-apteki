namespace GrafikPlanerData.Models;

public class SettingsRecord
{
    public int Id { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public bool MondayOpen { get; set; }
    public bool TuesdayOpen { get; set; }
    public bool WednesdayOpen { get; set; }
    public bool ThursdayOpen { get; set; }
    public bool FridayOpen { get; set; }
    public bool SaturdayOpen { get; set; }
    public bool SundayOpen { get; set; }

    // Per-day opening/closing times
    public TimeOnly MondayOpeningTime { get; set; }
    public TimeOnly MondayClosingTime { get; set; }
    public TimeOnly TuesdayOpeningTime { get; set; }
    public TimeOnly TuesdayClosingTime { get; set; }
    public TimeOnly WednesdayOpeningTime { get; set; }
    public TimeOnly WednesdayClosingTime { get; set; }
    public TimeOnly ThursdayOpeningTime { get; set; }
    public TimeOnly ThursdayClosingTime { get; set; }
    public TimeOnly FridayOpeningTime { get; set; }
    public TimeOnly FridayClosingTime { get; set; }
    public TimeOnly SaturdayOpeningTime { get; set; }
    public TimeOnly SaturdayClosingTime { get; set; }
    public TimeOnly SundayOpeningTime { get; set; }
    public TimeOnly SundayClosingTime { get; set; }

    public (TimeOnly Opening, TimeOnly Closing) GetHoursForDay(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => (MondayOpeningTime, MondayClosingTime),
            DayOfWeek.Tuesday => (TuesdayOpeningTime, TuesdayClosingTime),
            DayOfWeek.Wednesday => (WednesdayOpeningTime, WednesdayClosingTime),
            DayOfWeek.Thursday => (ThursdayOpeningTime, ThursdayClosingTime),
            DayOfWeek.Friday => (FridayOpeningTime, FridayClosingTime),
            DayOfWeek.Saturday => (SaturdayOpeningTime, SaturdayClosingTime),
            DayOfWeek.Sunday => (SundayOpeningTime, SundayClosingTime),
            _ => (OpeningTime, ClosingTime)
        };
    }

    public bool IsDayOpen(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => MondayOpen,
            DayOfWeek.Tuesday => TuesdayOpen,
            DayOfWeek.Wednesday => WednesdayOpen,
            DayOfWeek.Thursday => ThursdayOpen,
            DayOfWeek.Friday => FridayOpen,
            DayOfWeek.Saturday => SaturdayOpen,
            DayOfWeek.Sunday => SundayOpen,
            _ => true
        };
    }
}
