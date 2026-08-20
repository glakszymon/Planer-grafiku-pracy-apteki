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
        var workingHours = settingsTable.GetSettings();
        
        var emptyHours = new Dictionary<TimeOnly, bool>();
        
        for (var i = workingHours.OpeningTime; i < workingHours.ClosingTime; i = i.AddHours(1))
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