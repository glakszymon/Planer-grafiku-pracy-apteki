using GrafikPlanerData.Models;
using  Microsoft.Data.Sqlite;

namespace GrafikPlanerData.DbScripts;

public class ShiftTable : DbConnectionOption
{
    public void CreateTable()
    {
        var createShiftsTableCommand = _connection.CreateCommand();
        
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
        var command = _connection.CreateCommand();
        
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
            
        var command = _connection.CreateCommand();
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
            shiftRecord.ShiftDate = DateOnly.Parse(reader.GetString(reader.GetOrdinal("ShiftDate")));
            shiftRecord.PoleColor = reader.IsDBNull(reader.GetOrdinal("PoleColor")) ? null : reader.GetString(reader.GetOrdinal("PoleColor"));
            shiftRecord.PoleIcon = reader.IsDBNull(reader.GetOrdinal("PoleIcon")) ? null : reader.GetString(reader.GetOrdinal("PoleIcon"));
            
            shifts.Add(shiftRecord);
        }
        return shifts;
    }
    
    public void UpdateShiftRecord(ShiftRecord record)
    {
        var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE ShiftRecords
                SET ShiftHourId  = @shiftHourId, PoleColor = @poleColor, PoleIcon = @poleIcon
                WHERE Id = @id;";
        command.Parameters.AddWithValue("@shiftHourId", (object?)record.ShiftHourId ?? DBNull.Value);
        command.Parameters.AddWithValue("@poleColor", (object?)record.PoleColor ?? DBNull.Value);
        command.Parameters.AddWithValue("@poleIcon", (object?)record.PoleIcon ?? DBNull.Value);
        command.Parameters.AddWithValue("@id", record.Id);
        command.ExecuteNonQuery();
    }

    public int NumberOfRecordsInMonthSchedule(int month, int year)
    {
        string monthPattern = $"{year:D4}-{month:D2}-%";
    
        using var command = _connection.CreateCommand();
        command.CommandText = @"
        SELECT COUNT(ShiftRecords.Id) 
        FROM ShiftRecords 
        WHERE ShiftDate LIKE @monthPattern";
    
        // Prawidłowe dodanie parametru w natywnym ADO.NET
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@monthPattern";
        parameter.Value = monthPattern;
        command.Parameters.Add(parameter);
    
        // ExecuteScalar pobiera pierwszą kolumnę z pierwszego wiersza
        var result = command.ExecuteScalar();

        // Wykonujemy bezpieczną konwersję (na wypadek null)
        return result != null && result != DBNull.Value 
            ? Convert.ToInt32(result) 
            : 0;
    }

    public List<ScheduleInfo> GetAllSchedulesDates()
    {
        var schedules = new List<ScheduleInfo>();

        var command = _connection.CreateCommand();
        command.CommandText = @"SELECT substr(ShiftDate, 1, 7) as date FROM ShiftRecords GROUP BY date ORDER BY date DESC ;";
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(reader.GetOrdinal("date")))
            {
                string dateStr = reader.GetString(reader.GetOrdinal("date"));

                if (DateOnly.TryParse($"{dateStr}-01", out var prasedDate))
                {
                    int year = prasedDate.Year;
                    int month = prasedDate.Month;
                    
                    schedules.Add(new ScheduleInfo(month, year));
                }
            }
        }
        return schedules;
    }

    public void DeleteSchedule(int month, int year)
    {
        string monthPattern = $"{year:D4}-{month:D2}-%";

        var command = _connection.CreateCommand();
        command.CommandText = @"DELETE FROM ShiftRecords WHERE ShiftDate LIKE @monthPattern;";
        command.Parameters.AddWithValue("@monthPattern", monthPattern);
        command.ExecuteNonQuery();
    }
}