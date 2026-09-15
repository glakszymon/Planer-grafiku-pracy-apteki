using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;
using GrafikPlanerData.Models.Enums;

namespace GrafikPlanerCore;

public class ScheduleAnalisation
{
    public SettingsRecord _settings { get; set; } = new();
    private HashSet<int> _absenceHourIds = new();
    private HashSet<DateOnly> _holidayDates = new();
    private Dictionary<int, EmploymentType> _employeeEmploymentTypes = new();
    private Dictionary<int, HoursRecord> _hoursById = new();

    private const double MinDailyRestHours = 11.0;
    
    public List<DateTime> CheckEmptyHoursInSchedule(List<ScheduleRow> scheduleRows, int month, int year)
    {
        GetSettings();
        LoadAbsenceHourIds();
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

    private void LoadAbsenceHourIds()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        _absenceHourIds = hoursTable.GetAllHours()
            .Where(h => h.IsVacation || h.IsSickLeave)
            .Select(h => h.Id)
            .ToHashSet();
    }

    private void LoadHolidays(int year, int month)
    {
        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        _holidayDates = PolishHolidays.GetActiveHolidayDatesForMonth(holidaysTable, year, month);
    }

    private void LoadEmployeeEmploymentTypes()
    {
        var employeeTable = new EmployeeTable();
        employeeTable.StartConnectionWithDatabase();
        _employeeEmploymentTypes = employeeTable.GetAllEmployees()
            .GroupBy(e => e.Id)
            .ToDictionary(g => g.Key, g => g.First().EmploymentType);
    }

    private void LoadHoursById()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        _hoursById = hoursTable.GetAllHours()
            .GroupBy(h => h.Id)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public List<DateTime> CheckOneDay(List<ScheduleRow> scheduleRows, DateOnly targetDate)
    {
        // Skip closed days and holidays
        if (!_settings.IsDayOpen(targetDate.DayOfWeek) || _holidayDates.Contains(targetDate))
            return new List<DateTime>();

        var ans = new List<DateTime>();
    
        var shiftsForDay = scheduleRows
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate 
                             && record.StartTime.HasValue 
                             && record.EndTime.HasValue
                             && (!record.ShiftHourId.HasValue || !_absenceHourIds.Contains(record.ShiftHourId.Value)))
            .ToList();

        var (dayOpen, dayClose) = _settings.GetHoursForDay(targetDate.DayOfWeek);

        for (var i = dayOpen; i < dayClose; i = i.AddHours(1))
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
        LoadAbsenceHourIds();
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
        if (!_settings.IsDayOpen(targetDate.DayOfWeek) || _holidayDates.Contains(targetDate))
            return new List<DateTime>();

        var ans = new List<DateTime>();

        var pharmacistShifts = scheduleRows
            .Where(row => row.Specialisation == "Farmaceuta/ka")
            .SelectMany(row => row.Records ?? new List<ScheduleColumn>())
            .Where(record => record.ShiftDate == targetDate
                             && record.StartTime.HasValue
                             && record.EndTime.HasValue
                             && (!record.ShiftHourId.HasValue || !_absenceHourIds.Contains(record.ShiftHourId.Value)))
            .ToList();

        var (pharmOpen, pharmClose) = _settings.GetHoursForDay(targetDate.DayOfWeek);

        for (var i = pharmOpen; i < pharmClose; i = i.AddHours(1))
        {
            bool pharmacistPresent = pharmacistShifts.Any(z => z.StartTime <= i && z.EndTime >= i.AddHours(1));
            if (!pharmacistPresent)
            {
                ans.Add(targetDate.ToDateTime(i));
            }
        }

        return ans;
    }

    public List<DailyRestViolation> CheckDailyRestInSchedule(List<ScheduleRow> scheduleRows, int month, int year)
    {
        GetSettings();
        LoadAbsenceHourIds();
        LoadHoursById();
        LoadEmployeeEmploymentTypes();

        var violations = new List<DailyRestViolation>();

        foreach (var row in scheduleRows)
        {
            if (row.Id < 0) continue;
            if (!_employeeEmploymentTypes.TryGetValue(row.Id, out var type) || type != EmploymentType.UmowaPrace)
                continue;

            violations.AddRange(CheckDailyRestForEmployee(row, month, year));
        }

        return violations;
    }

    private List<DailyRestViolation> CheckDailyRestForEmployee(ScheduleRow row, int month, int year)
    {
        var violations = new List<DailyRestViolation>();

        var recordsByDate = (row.Records ?? new List<ScheduleColumn>())
            .GroupBy(r => r.ShiftDate)
            .ToDictionary(g => g.Key, g => g.First());

        // Previous work shift (last calendar day) by date
        Dictionary<DateOnly, ScheduleColumn> workShifts = new();
        foreach (var (date, record) in recordsByDate)
        {
            if (IsWorkShift(record))
                workShifts[date] = record;
        }

        // Boundary: previous month's last work shift for day 1
        ScheduleColumn? prevMonthShift = GetPreviousMonthLastWorkShift(row.Id, month, year);

        // Check previous month boundary -> day 1
        if (prevMonthShift != null)
        {
            var day1 = new DateOnly(year, month, 1);
            // Only applies if the previous shift is on the immediately preceding calendar day
            if (day1.DayNumber - prevMonthShift.ShiftDate.DayNumber == 1 &&
                workShifts.TryGetValue(day1, out var day1Shift))
            {
                var violation = CheckRestBetween(row, prevMonthShift, day1Shift, prevMonthShift.ShiftDate, day1);
                if (violation != null)
                    violations.Add(violation);
            }
        }

        // Check consecutive work days within the month
        var orderedDates = workShifts.Keys.OrderBy(d => d).ToList();
        for (int i = 1; i < orderedDates.Count; i++)
        {
            var current = orderedDates[i - 1];
            var next = orderedDates[i];

            // Only adjacent calendar days are compared; a gap day (day off/vacation) resets the counter
            if (next.DayNumber - current.DayNumber != 1)
                continue;

            var violation = CheckRestBetween(row, workShifts[current], workShifts[next], current, next);
            if (violation != null)
                violations.Add(violation);
        }

        return violations;
    }

    private DailyRestViolation? CheckRestBetween(ScheduleRow row, ScheduleColumn prev, ScheduleColumn next, DateOnly prevDay, DateOnly nextDay)
    {
        if (!prev.StartTime.HasValue || !prev.EndTime.HasValue)
            return null;
        if (!next.StartTime.HasValue || !next.EndTime.HasValue)
            return null;

        var gapHours = (nextDay.ToDateTime(next.StartTime.Value) - prevDay.ToDateTime(prev.EndTime.Value)).TotalHours;
        if (gapHours < MinDailyRestHours)
        {
            return new DailyRestViolation
            {
                EmployeeId = next.EmployeeId,
                EmployeeName = $"{row.FirstName} {row.LastName}".Trim(),
                Day = nextDay,
                PrevDay = prevDay,
                Message = $"{prevDay.ToString("dd.MM")} {prev.EndTime.Value:HH:mm} -> {nextDay.ToString("dd.MM")} {next.StartTime.Value:HH:mm} ({gapHours:0.#} h)"
            };
        }

        return null;
    }

    private bool IsWorkShift(ScheduleColumn record)
    {
        if (!record.StartTime.HasValue || !record.EndTime.HasValue)
            return false;
        if (record.ShiftHourId.HasValue && _absenceHourIds.Contains(record.ShiftHourId.Value))
            return false;
        return true;
    }

    private ScheduleColumn? GetPreviousMonthLastWorkShift(int employeeId, int month, int year)
    {
        var prevYear = year;
        var prevMonth = month - 1;
        if (prevMonth == 0)
        {
            prevMonth = 12;
            prevYear = year - 1;
        }

        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        var shift = shiftTable.GetLastShiftInMonth(prevMonth, prevYear, employeeId);
        if (shift == null)
            return null;

        if (shift.ShiftHourId is 0 or null)
            return null;

        if (_absenceHourIds.Contains(shift.ShiftHourId.Value))
            return null;

        if (!_hoursById.TryGetValue(shift.ShiftHourId.Value, out var hour))
            return null;

        return new ScheduleColumn
        {
            Id = shift.Id,
            EmployeeId = shift.EmployeeId,
            ShiftDate = shift.ShiftDate,
            ShiftHourId = shift.ShiftHourId,
            StartTime = hour.StartTime,
            EndTime = hour.EndTime,
            Symbol = hour.Symbol
        };
    }
}
