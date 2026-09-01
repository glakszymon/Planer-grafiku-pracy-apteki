using GrafikPlanerData.DbScripts;

namespace GrafikPlanerCore;

public class SettingsAnalisation
{
    public List<TimeOnly> CheckWorkshiftsHours()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        var shifts = hoursTable.GetAllHours();
        
        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        var settings = settingsTable.GetSettings();
        
        // Find the widest opening range across all open days
        TimeOnly earliest = new TimeOnly(23, 59);
        TimeOnly latest = new TimeOnly(0, 0);
        
        var allDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                              DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        
        foreach (var day in allDays)
        {
            if (!settings.IsDayOpen(day)) continue;
            var (open, close) = settings.GetHoursForDay(day);
            if (open < earliest) earliest = open;
            if (close > latest) latest = close;
        }
        
        if (earliest >= latest) return new List<TimeOnly>();

        var emptyHours = new Dictionary<TimeOnly, bool>();
        
        for (var i = earliest; i < latest; i = i.AddHours(1))
        {
            emptyHours[i] = false;
        }

        foreach (var shiftRecord in shifts.Where(s => !s.IsVacation))
        {
            for (var i = shiftRecord.StartTime; i < shiftRecord.EndTime; i = i.AddHours(1))
            {
                if (emptyHours.ContainsKey(i))
                {
                    emptyHours[i] = true;
                }
            }
        }

        var result = new List<TimeOnly>();
        
        foreach (var e in emptyHours)
        {
            if (!e.Value)
            {
                result.Add(e.Key);
            }
        }
        
        return result;
    }
}
