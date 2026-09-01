using Microsoft.Data.Sqlite;
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
        
        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        settingsTable.CreateTable();
        settingsTable.InitializeSettings();
        
        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        holidaysTable.CreateTable();
        
        RunMigrations();
    }
    
    private void RunMigrations()
    {
        using var connection = new SqliteConnection("Data Source=apteka.db");
        connection.Open();
        
        // Migration: Add IsVacation column to ShiftHours if missing
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE ShiftHours ADD COLUMN IsVacation INTEGER NOT NULL DEFAULT 0";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // Column already exists — ignore
        }
    }
}