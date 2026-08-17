using GrafikPlanerData.DbScripts;

namespace GrafikPlanerData;

public class DbInitialization
{
    public void InitializeDatabase()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        hoursTable.CreateTable();
        
        var employeeTable = new EmployeeTable();
        employeeTable.StartConnectionWithDatabase();
        employeeTable.CreateTable();
        
        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        shiftTable.CreateTable();
    }
}

// TODO: dodaj do bazy danych tabele z ustawieniami które obsługuja godziny otwarcia apteki, dni działania apteki, ustawienia godzin pracy i ustawienie oznaczeń 