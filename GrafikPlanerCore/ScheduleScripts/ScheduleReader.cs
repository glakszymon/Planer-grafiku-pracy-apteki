using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore.ScheduleScripts;

public class ScheduleReader
{
    private List<EmployeeRecord> _employeeRecords = new List<EmployeeRecord>();
    private List<ShiftRecord> _shiftRecords = new List<ShiftRecord>();
    private List<HoursRecord> _hoursRecords = new List<HoursRecord>();
    private List<HolidayRecord> _holidays = new List<HolidayRecord>();
    private Dictionary<int, (int UsedInYear, int UsedPrevYear)> _vacationUsages = new();
    private int _month;
    private int _year;

    public List<ScheduleRow> FinalSchedule = new List<ScheduleRow>();
    
    public void GetDataFromDb(int month, int year)
    {
        _month = month;
        _year = year;
        
        var employeeTable = new EmployeeTable();
        employeeTable.StartConnectionWithDatabase();
        _employeeRecords = employeeTable.GetAllEmployees();
        
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        _hoursRecords = hoursTable.GetAllHours();
        
        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        _shiftRecords = shiftTable.GetShiftRecordsOfMonth(month, year);
        _vacationUsages = shiftTable.GetVacationUsages(year, year - 1);
        
        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        PolishHolidays.SeedBuiltIn(holidaysTable);
        _holidays = holidaysTable.GetAllActiveHolidays();
    }

    public void TransformDataToTableStructure()
    {
        var joinedTables =  (
            from shift in _shiftRecords
            join hour in _hoursRecords on shift.ShiftHourId equals hour.Id into hourGroup
            from hour in hourGroup.DefaultIfEmpty()
            select new ScheduleColumn
            {
                Id = shift.Id,
                EmployeeId = shift.EmployeeId,
                ShiftDate = shift.ShiftDate,
                PoleColor = shift.PoleColor,
                PoleIcon = shift.PoleIcon,
                
                ShiftHourId = hour?.Id,
                StartTime = hour?.StartTime,
                EndTime = hour?.EndTime,
                Symbol = hour?.Symbol ?? string.Empty
            }).ToList();
        
        List<ScheduleRow> tempSchedule =  _employeeRecords.Select(emp =>
        {
            // Pobieramy wszystkie zmiany przypisane do danego pracownika
            var employeeShifts = joinedTables
                .Where(s => s.EmployeeId == emp.Id)
                .ToList();

            // Wyliczamy sumę godzin (przykład: jeśli różnica w czasie tworzy godziny)
            // Lub sumujemy bezpośrednio z Twojej logiki biznesowej
            int totalHours = employeeShifts
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Sum(s => (int)(s.EndTime!.Value - s.StartTime!.Value).TotalHours);

            // Stan urlopu dla roku otwartego grafiku: wykorzystane liczymy z rekordów zmian,
            // zaległe z poprzedniego roku wg miesiąca referencyjnego (aktywne do września).
            int usedInYear = _vacationUsages.TryGetValue(emp.Id, out var usage) ? usage.UsedInYear : 0;
            int usedPrevYear = _vacationUsages.TryGetValue(emp.Id, out usage) ? usage.UsedPrevYear : 0;
            int quota = emp.VacationDays ?? 0;
            var vacationState = VacationStateCalculator.Compute(quota, usedInYear, usedPrevYear, _month);

            return new ScheduleRow
            {
                Id = emp.Id,
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                Specialisation = emp.Specialisation,
                Email = emp.Email,
                PhoneNumber = emp.PhoneNumber,
                VacationDays = emp.VacationDays,
                UsedVacationDays = usedInYear,
                UnusedVacationDaysFromLastYear = vacationState.Carryover,
                
                HoursSummary = totalHours,
                ExpectedHours = ExpectedHoursCalculator.Calculate(_year, _month, emp.WorkTimeRate, _holidays),
                Records = employeeShifts
            };
        }).ToList();
        
        FinalSchedule =  tempSchedule;
    }

    public bool SheduleExistInDb(int month, int year)
    {
        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        int numberOfRecords = shiftTable.NumberOfRecordsInMonthSchedule(month, year);

        if (numberOfRecords > 0)
        {
            return true;
        }
        return false;
    }
}