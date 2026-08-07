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

    public List<ShiftRecord> GetShiftRecordsOfMonth(int month, int year)
    {
        var shifts = new List<ShiftRecord>();
        string monthPattern = $"{year:D4}-{month:D2}-%";
            
        var command = _activeConnection.CreateCommand();
        command.CommandText = @"
            SELECT ShiftRecords.Id, ShiftRecords.EmployeeId, ShiftRecords.ShiftHourId, ShiftRecords.ShiftDate, ShiftRecords.PoleColor, ShiftRecords.PoleIcon
            FROM ShiftRecords WHERE ShiftDate LIKE @monthPattern";
        
        command.Parameters.AddWithValue("@monthPattern", monthPattern);
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var shiftRecord = new ShiftRecord();
            shiftRecord.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            shiftRecord.EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId"));
            shiftRecord.ShiftHourId = reader.IsDBNull(reader.GetOrdinal("ShiftHourId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ShiftHourId"));
            /*shiftRecord.ShiftDate = reader.GetString(reader.GetOrdinal("ShiftDate"));*/
            /*TODO: dodaj formatowanie string na date */
            shiftRecord.PoleColor = reader.IsDBNull(reader.GetOrdinal("PoleColor")) ? null : reader.GetString(reader.GetOrdinal("PoleColor"));
            shiftRecord.PoleIcon = reader.IsDBNull(reader.GetOrdinal("PoleIcon")) ? null : reader.GetString(reader.GetOrdinal("PoleIcon"));
            
            shifts.Add(shiftRecord);
        }
        return shifts;
    }
}