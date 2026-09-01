using GrafikPlanerData.Models;

namespace GrafikPlanerData.DbScripts;

public class HolidaysTable : DbConnectionOption
{
    public void CreateTable()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Holidays (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                IsBuiltIn INTEGER NOT NULL DEFAULT 0 CHECK (IsBuiltIn IN (0, 1)),
                IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
                Month INTEGER,
                Day INTEGER,
                EasterOffset INTEGER
            );";
        command.ExecuteNonQuery();
    }

    public List<HolidayRecord> GetAllHolidays()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM Holidays ORDER BY Month, Day, EasterOffset;";
        return ReadHolidays(command);
    }

    public List<HolidayRecord> GetAllActiveHolidays()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT * FROM Holidays WHERE IsActive = 1 ORDER BY Month, Day, EasterOffset;";
        return ReadHolidays(command);
    }

    public void InsertHoliday(HolidayRecord holiday)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Holidays (Name, IsBuiltIn, IsActive, Month, Day, EasterOffset)
            VALUES (@name, @isBuiltIn, @isActive, @month, @day, @easterOffset);";
        command.Parameters.AddWithValue("@name", holiday.Name);
        command.Parameters.AddWithValue("@isBuiltIn", holiday.IsBuiltIn ? 1 : 0);
        command.Parameters.AddWithValue("@isActive", holiday.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@month", (object?)holiday.Month ?? DBNull.Value);
        command.Parameters.AddWithValue("@day", (object?)holiday.Day ?? DBNull.Value);
        command.Parameters.AddWithValue("@easterOffset", (object?)holiday.EasterOffset ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public bool ExistsBuiltIn(int? month, int? day, int? easterOffset)
    {
        using var command = _connection.CreateCommand();
        if (month.HasValue && day.HasValue)
        {
            command.CommandText = "SELECT COUNT(*) FROM Holidays WHERE IsBuiltIn = 1 AND Month = @month AND Day = @day;";
            command.Parameters.AddWithValue("@month", month.Value);
            command.Parameters.AddWithValue("@day", day.Value);
        }
        else if (easterOffset.HasValue)
        {
            command.CommandText = "SELECT COUNT(*) FROM Holidays WHERE IsBuiltIn = 1 AND EasterOffset = @easterOffset;";
            command.Parameters.AddWithValue("@easterOffset", easterOffset.Value);
        }
        else
        {
            return false;
        }
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void DeleteHoliday(int id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM Holidays WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public void UpdateHolidayActive(int id, bool isActive)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "UPDATE Holidays SET IsActive = @isActive WHERE Id = @id;";
        command.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private List<HolidayRecord> ReadHolidays(Microsoft.Data.Sqlite.SqliteCommand command)
    {
        var holidays = new List<HolidayRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var monthOrd = reader.GetOrdinal("Month");
            var dayOrd = reader.GetOrdinal("Day");
            var easterOrd = reader.GetOrdinal("EasterOffset");
            
            holidays.Add(new HolidayRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                IsBuiltIn = reader.GetInt32(reader.GetOrdinal("IsBuiltIn")) == 1,
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                Month = reader.IsDBNull(monthOrd) ? null : reader.GetInt32(monthOrd),
                Day = reader.IsDBNull(dayOrd) ? null : reader.GetInt32(dayOrd),
                EasterOffset = reader.IsDBNull(easterOrd) ? null : reader.GetInt32(easterOrd),
            });
        }
        return holidays;
    }
}
