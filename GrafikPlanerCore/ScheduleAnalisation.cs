using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore;

public class ScheduleAnalisation
{
    public SettingsRecord _settings { get; set; } = new();
    
    public List<DateTime> CheckEmptyHoursInSchedule(List<ScheduleRow> scheduleRows, int month, int year)
    {
        GetSettings();
        
        List<DateTime> emptyHours = new List<DateTime>();
        var numberOfDaysInMonth = DateTime.DaysInMonth(year, month);

        for (int i = 1; i <= numberOfDaysInMonth; i++)
        {
            var dates = CheckOneDay(scheduleRows, new DateOnly(year, month, i));
            emptyHours.AddRange(dates);
        }
        
        return emptyHours;
    }

    public void GetSettings()
    {
        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        _settings = settingsTable.GetSettings();
    }

    public List<DateTime> CheckOneDay(List<ScheduleRow> scheduleRows, DateOnly targetDate)
    {
        // Skip closed days
        if (!IsDayOpen(targetDate.DayOfWeek))
            return new List<DateTime>();

        var ans = new List<DateTime>();
    
        var shiftsForDay = scheduleRows
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate 
                             && record.StartTime.HasValue 
                             && record.EndTime.HasValue)
            .ToList();

        for (var i = _settings.OpeningTime; i < _settings.ClosingTime; i = i.AddHours(1))
        {
            bool ktosPracuje = shiftsForDay.Any(z => z.StartTime <= i && z.EndTime >= i.AddHours(1));
        
            if (!ktosPracuje)
            {
                DateTime fullDateTime = targetDate.ToDateTime(i);
                ans.Add(fullDateTime);
            }
        }

        return ans;
    }

    private bool IsDayOpen(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => _settings.MondayOpen,
            DayOfWeek.Tuesday => _settings.TuesdayOpen,
            DayOfWeek.Wednesday => _settings.WednesdayOpen,
            DayOfWeek.Thursday => _settings.ThursdayOpen,
            DayOfWeek.Friday => _settings.FridayOpen,
            DayOfWeek.Saturday => _settings.SaturdayOpen,
            DayOfWeek.Sunday => _settings.SundayOpen,
            _ => true
        };
    }
}
