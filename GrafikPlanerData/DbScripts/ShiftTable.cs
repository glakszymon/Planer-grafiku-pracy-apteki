using GrafikPlanerData.Models;
using  Microsoft.Data.Sqlite;

namespace GrafikPlanerData.DbScripts;

public class ShiftTable
{
    SqliteConnection  _activeConnection;

    public ShiftTable(SqliteConnection connection)
    {
        _activeConnection = connection;
    }

    public void CreateTable()
    {
        var createShiftsTableCommand = _activeConnection.CreateCommand();
        
        createShiftsTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS ShiftRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                ShiftHourId INTEGER,
                ShiftDate TEXT NOT NULL,
                PoleColor TEXT,
                PoleIcon TEXT,
                FOREIGN KEY (EmployeeId) REFERENCES Employee(Id),
                FOREIGN KEY (ShiftHourId) REFERENCES ShiftHours(Id)
            );";
        
        createShiftsTableCommand.ExecuteNonQuery();
    }

    public void AddShift(ShiftRecord record)
    {
        var command = _activeConnection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO ShiftRecords (EmployeeId, ShiftHourId, ShiftDate, PoleColor, PoleIcon) 
            VALUES (@employeeId, @shiftHourId, @shiftDate, @poleColor, @poleIcon);";

        command.Parameters.AddWithValue("@employeeId", record.EmployeeId);
        command.Parameters.AddWithValue("@shiftHourId", (object?)record.ShiftHourId ?? DBNull.Value);
        command.Parameters.AddWithValue("@shiftDate", record.ShiftDate); 
        command.Parameters.AddWithValue("@poleColor", (object?)record.PoleColor ?? DBNull.Value);
        command.Parameters.AddWithValue("@poleIcon", (object?)record.PoleIcon ?? DBNull.Value);

        command.ExecuteNonQuery();
    }
}