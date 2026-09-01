using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore;

public class ScheduleAnalisation
{
    public SettingsRecord _settings { get; set; } = new();
    private HashSet<int> _vacationHourIds = new();
    private HashSet<DateOnly> _holidayDates = new();
    
    public List<DateTime> CheckEmptyHoursInSchedule(List<ScheduleRow> scheduleRows, int month, int year)
    {
        GetSettings();
        LoadVacationHourIds();
        LoadHolidays(year, month);
        
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

    private void LoadVacationHourIds()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        _vacationHourIds = hoursTable.GetAllHours()
            .Where(h => h.IsVacation)
            .Select(h => h.Id)
            .ToHashSet();
    }

    private void LoadHolidays(int year, int month)
    {
        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        _holidayDates = PolishHolidays.GetActiveHolidayDatesForMonth(holidaysTable, year, month);
    }

    public List<DateTime> CheckOneDay(List<ScheduleRow> scheduleRows, DateOnly targetDate)
    {
        // Skip closed days and holidays
        if (!IsDayOpen(targetDate.DayOfWeek) || _holidayDates.Contains(targetDate))
            return new List<DateTime>();

        var ans = new List<DateTime>();
    
        var shiftsForDay = scheduleRows
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate 
                             && record.StartTime.HasValue 
                             && record.EndTime.HasValue
                             && (!record.ShiftHourId.HasValue || !_vacationHourIds.Contains(record.ShiftHourId.Value)))
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

    public List<DateTime> CheckPharmacistGapsInSchedule(List<ScheduleRow> scheduleRows, int month, int year)
    {
        GetSettings();
        LoadVacationHourIds();
        LoadHolidays(year, month);
        
        List<DateTime> emptyHours = new List<DateTime>();
        var numberOfDaysInMonth = DateTime.DaysInMonth(year, month);

        for (int i = 1; i <= numberOfDaysInMonth; i++)
        {
            var dates = CheckOneDayForPharmacist(scheduleRows, new DateOnly(year, month, i));
            emptyHours.AddRange(dates);
        }
        
        return emptyHours;
    }

    public List<DateTime> CheckOneDayForPharmacist(List<ScheduleRow> scheduleRows, DateOnly targetDate)
    {
        if (!IsDayOpen(targetDate.DayOfWeek) || _holidayDates.Contains(targetDate))
            return new List<DateTime>();

        var ans = new List<DateTime>();

        var pharmacistShifts = scheduleRows
            .Where(row => row.Specialisation == "Farmaceuta/ka")
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate
                             && record.StartTime.HasValue
                             && record.EndTime.HasValue
                             && (!record.ShiftHourId.HasValue || !_vacationHourIds.Contains(record.ShiftHourId.Value)))
            .ToList();

        for (var i = _settings.OpeningTime; i < _settings.ClosingTime; i = i.AddHours(1))
        {
            bool pharmacistPresent = pharmacistShifts.Any(z => z.StartTime <= i && z.EndTime >= i.AddHours(1));
            if (!pharmacistPresent)
            {
                ans.Add(targetDate.ToDateTime(i));
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
