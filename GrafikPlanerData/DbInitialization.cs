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
        
        // Migration: Recreate Holidays table with recurring schema (Month/Day/EasterOffset instead of Date)
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Holidays') WHERE name = 'Date';";
            var hasDateColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
            
            if (hasDateColumn)
            {
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = "DROP TABLE Holidays;";
                dropCmd.ExecuteNonQuery();
                
                // Table will be recreated by CreateTable() on next init
                var holidaysTable = new HolidaysTable();
                holidaysTable.StartConnectionWithDatabase();
                holidaysTable.CreateTable();
            }
        }
        catch (SqliteException)
        {
            // Table doesn't exist yet — ignore
        }
        
        // Migration: Add per-day opening/closing time columns to Settings
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        foreach (var day in dayNames)
        {
            foreach (var suffix in new[] { "OpeningTime", "ClosingTime" })
            {
                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"ALTER TABLE Settings ADD COLUMN {day}{suffix} TEXT";
                    cmd.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Column already exists — ignore
                }
            }
        }
        
        // Backfill: copy global times to per-day columns where null
        try
        {
            using var cmd = connection.CreateCommand();
            var setClauses = new List<string>();
            foreach (var day in dayNames)
            {
                setClauses.Add($"{day}OpeningTime = COALESCE({day}OpeningTime, OpeningTime)");
                setClauses.Add($"{day}ClosingTime = COALESCE({day}ClosingTime, ClosingTime)");
            }
            cmd.CommandText = $"UPDATE Settings SET {string.Join(", ", setClauses)} WHERE Id = 1;";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // ignore
        }
    }
}