using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerCore.ScheduleScripts;

public class ScheduleCreator
{
    public bool ScheduleIsExisting(int month, int year)
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
    
    private List<ShiftRecord> _generatedRecords = new List<ShiftRecord>();

    public void GenerateRecords(int month, int year)
    {
        int numberOfDaysInMonth = DateTime.DaysInMonth(year, month);
        
        var employees =  ReadListOfEmployees();

        foreach (var employee in employees)
        {
            for (int i = 1; i <= numberOfDaysInMonth; i++)
            {
                var tempShiftRecord = new ShiftRecord();
                tempShiftRecord.EmployeeId = employee.Id;
                tempShiftRecord.ShiftDate = new DateOnly(year, month, i);
                
                _generatedRecords.Add(tempShiftRecord);
            }
        }
    }

    private List<EmployeeRecord> ReadListOfEmployees()
    {
        var table = new EmployeeTable();
        table.StartConnectionWithDatabase();
        return table.GetAllEmployees();
    }
    
    public void SendScheduleToDatabase()
    {
        var table = new ShiftTable();
        table.StartConnectionWithDatabase();
        foreach (var shift in _generatedRecords)
        {
            table.AddShift(shift);
        }
    }
}