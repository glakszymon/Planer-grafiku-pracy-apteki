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
        
        System.Diagnostics.Debug.WriteLine($"[ANALISATION] Settings loaded: OpeningTime={_settings.OpeningTime}, ClosingTime={_settings.ClosingTime}");
        System.Diagnostics.Debug.WriteLine($"[ANALISATION] Checking month={month}, year={year}, rows={scheduleRows.Count}");
        
        List<DateTime> emptyHours = new List<DateTime>();
        var numberOfDaysInMonth = DateTime.DaysInMonth(year, month);

        for (int i = 1; i <= numberOfDaysInMonth; i++)
        {
            var dates = CheckOneDay(scheduleRows, new DateOnly(year, month, i));
            emptyHours.AddRange(dates);
        }
        
        System.Diagnostics.Debug.WriteLine($"[ANALISATION] Total empty hour slots found: {emptyHours.Count}");
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
        var ans = new List<DateTime>();
    
        var shiftsForDay = scheduleRows
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate 
                             && record.StartTime.HasValue 
                             && record.EndTime.HasValue)
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[ANALISATION] CheckOneDay date={targetDate}, shiftsForDay={shiftsForDay.Count}, OpeningTime={_settings.OpeningTime}, ClosingTime={_settings.ClosingTime}");

        for (var i = _settings.OpeningTime; i < _settings.ClosingTime; i = i.AddHours(1))
        {
            bool ktosPracuje = shiftsForDay.Any(z => z.StartTime <= i && z.EndTime >= i.AddHours(1));
        
            if (!ktosPracuje)
            {
                DateTime fullDateTime = targetDate.ToDateTime(i);
                ans.Add(fullDateTime);
            }
        }

        if (ans.Count > 0)
            System.Diagnostics.Debug.WriteLine($"[ANALISATION] CheckOneDay date={targetDate} => {ans.Count} gaps");

        return ans;
    }
}