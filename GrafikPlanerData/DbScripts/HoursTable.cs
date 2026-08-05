using GrafikPlanerData.Models;
using Microsoft.Data.Sqlite;

namespace GrafikPlanerData.DbScripts;

public class HoursTable
{
    SqliteConnection  _activeConnection;
    
    public HoursTable(SqliteConnection connection)
    {
        _activeConnection = connection;
    }

    public void CreateTable()
    {
        var createHoursTableCommand = _activeConnection.CreateCommand();
        
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
        var command = _activeConnection.CreateCommand();
        
        command.CommandText = @"
            INSERT INTO ShiftHours (StartTime, EndTime, Symbol) 
            VALUES (@startTime, @endTime, @symbol);";

        command.Parameters.AddWithValue("@startTime", record.StartTime.ToString());
        command.Parameters.AddWithValue("@endTime", record.EndTime.ToString());
        command.Parameters.AddWithValue("@symbol", record.Symbol);

        command.ExecuteNonQuery();
    }
}