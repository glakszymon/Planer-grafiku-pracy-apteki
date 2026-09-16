using Microsoft.Data.Sqlite;
using GrafikPlanerData.DbScripts;

namespace GrafikPlanerData;

public class DbInitialization
{
    public void InitializeDatabase()
    {
        DatabasePath.EnsureMigrated();

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
        using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
        connection.Open();

        // Migration: convert legacy Holidays (Date TEXT) rows to recurring schema (Month/Day)
        // and drop the obsolete Date column — data is preserved, not deleted.
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Holidays') WHERE name = 'Date';";
            var hasDateColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

            if (hasDateColumn)
            {
                using var convertCmd = connection.CreateCommand();
                convertCmd.CommandText = @"
                    UPDATE Holidays
                    SET Month = CAST(substr(Date, 6, 2) AS INTEGER),
                        Day = CAST(substr(Date, 9, 2) AS INTEGER)
                    WHERE Date IS NOT NULL AND Month IS NULL AND Day IS NULL;";
                convertCmd.ExecuteNonQuery();

                using var dropDateCmd = connection.CreateCommand();
                dropDateCmd.CommandText = "ALTER TABLE Holidays DROP COLUMN Date;";
                dropDateCmd.ExecuteNonQuery();
            }
        }
        catch (SqliteException)
        {
            // Table doesn't exist or column not droppable — ignore
        }

        // Migration: remove legacy vacation columns from Employee that are no longer used.
        // YearOfVacationData was NOT NULL without a default, which crashed AddEmployee.
        try
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Employee') WHERE name = 'YearOfVacationData';";
            var hasLegacyVacationColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

            if (hasLegacyVacationColumn)
            {
                foreach (var column in new[] { "YearOfVacationData", "UsedVacationDays", "UnusedVacationDaysFromLastYear" })
                {
                    using var dropCmd = connection.CreateCommand();
                    dropCmd.CommandText = $"ALTER TABLE Employee DROP COLUMN {column};";
                    dropCmd.ExecuteNonQuery();
                }
            }
        }
        catch (SqliteException)
        {
            // Column already removed or not droppable — ignore
        }

        // Backfill: copy global times to per-day columns where null
        var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
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