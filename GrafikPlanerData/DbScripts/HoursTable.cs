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
                Symbol TEXT NOT NULL
            );";
        
        createHoursTableCommand.ExecuteNonQuery();
    }

    public void AddHours(HoursRecord record)
    {
        var command = _connection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO ShiftHours (StartTime, EndTime, Symbol) 
            VALUES (@startTime, @endTime, @symbol);";

        command.Parameters.AddWithValue("@startTime", record.StartTime.ToString());
        command.Parameters.AddWithValue("@endTime", record.EndTime.ToString());
        command.Parameters.AddWithValue("@symbol", record.Symbol);

        command.ExecuteNonQuery();
    }

    public List<HoursRecord> GetAllHours()
    {
        var hours = new List<HoursRecord>();
        
        var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, StartTime, EndTime, Symbol FROM ShiftHours";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var hour = new HoursRecord();
            hour.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            hour.Symbol = reader.GetString(reader.GetOrdinal("Symbol"));
            hour.StartTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("StartTime")));
            hour.EndTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("EndTime")));
            
            hours.Add(hour);
        }

        return hours;
    }


}