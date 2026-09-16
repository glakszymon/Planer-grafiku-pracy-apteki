using GrafikPlanerData.Models;
using Microsoft.Data.Sqlite;

namespace GrafikPlanerData.DbScripts;

public class HoursTable : DbConnectionOption
{


    public void CreateTable()
    {
        var createHoursTableCommand = _connection.CreateCommand();
        
        createHoursTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS ShiftHours (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                Symbol TEXT NOT NULL,
                IsVacation INTEGER NOT NULL DEFAULT 0,
                IsSickLeave INTEGER NOT NULL DEFAULT 0
            );";
        
        createHoursTableCommand.ExecuteNonQuery();

        EnsureColumns("ShiftHours",
            "StartTime TEXT",
            "EndTime TEXT",
            "Symbol TEXT",
            "IsVacation INTEGER NOT NULL DEFAULT 0",
            "IsSickLeave INTEGER NOT NULL DEFAULT 0");
    }

    public void AddHours(HoursRecord record)
    {
        var command = _connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO ShiftHours (StartTime, EndTime, Symbol, IsVacation, IsSickLeave) 
            VALUES (@startTime, @endTime, @symbol, @isVacation, @isSickLeave);";

        command.Parameters.AddWithValue("@startTime", record.StartTime.ToString());
        command.Parameters.AddWithValue("@endTime", record.EndTime.ToString());
        command.Parameters.AddWithValue("@symbol", record.Symbol);
        command.Parameters.AddWithValue("@isVacation", record.IsVacation ? 1 : 0);
        command.Parameters.AddWithValue("@isSickLeave", record.IsSickLeave ? 1 : 0);

        command.ExecuteNonQuery();
    }

    public List<HoursRecord> GetAllHours()
    {
        var hours = new List<HoursRecord>();
        
        var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, StartTime, EndTime, Symbol, IsVacation, IsSickLeave FROM ShiftHours";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var hour = new HoursRecord();
            hour.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            hour.Symbol = reader.GetString(reader.GetOrdinal("Symbol"));
            hour.StartTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("StartTime")));
            hour.EndTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("EndTime")));
            hour.IsVacation = reader.GetInt32(reader.GetOrdinal("IsVacation")) == 1;
            hour.IsSickLeave = reader.GetInt32(reader.GetOrdinal("IsSickLeave")) == 1;
            
            hours.Add(hour);
        }

        return hours;
    }

    public void UpdateHour(HoursRecord record)
    {
        var command = _connection.CreateCommand();
        
        command.CommandText = @"
            UPDATE ShiftHours SET 
                StartTime = @startTime, 
                EndTime = @endTime, 
                Symbol = @symbol,
                IsVacation = @isVacation,
                IsSickLeave = @isSickLeave
            WHERE Id = @id;";

        command.Parameters.AddWithValue("@startTime", record.StartTime.ToString());
        command.Parameters.AddWithValue("@endTime", record.EndTime.ToString());
        command.Parameters.AddWithValue("@symbol", record.Symbol);
        command.Parameters.AddWithValue("@isVacation", record.IsVacation ? 1 : 0);
        command.Parameters.AddWithValue("@isSickLeave", record.IsSickLeave ? 1 : 0);
        command.Parameters.AddWithValue("@id", record.Id);

        command.ExecuteNonQuery();
    }

    public void DeleteHour(HoursRecord record)
    {
        var command = _connection.CreateCommand();
        
        command.CommandText = @"
            DELETE FROM ShiftHours WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", record.Id);
        
        command.ExecuteNonQuery();
    }
    
}