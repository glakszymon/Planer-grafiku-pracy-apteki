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
                Date TEXT NOT NULL,
                Name TEXT NOT NULL,
                IsBuiltIn INTEGER NOT NULL DEFAULT 0 CHECK (IsBuiltIn IN (0, 1)),
                IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
            );";
        command.ExecuteNonQuery();
    }

    public List<HolidayRecord> GetHolidaysForYear(int year)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM Holidays 
            WHERE Date >= @startDate AND Date <= @endDate
            ORDER BY Date;";
        command.Parameters.AddWithValue("@startDate", new DateOnly(year, 1, 1).ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@endDate", new DateOnly(year, 12, 31).ToString("yyyy-MM-dd"));

        return ReadHolidays(command);
    }

    public List<HolidayRecord> GetActiveHolidaysForMonth(int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM Holidays 
            WHERE Date >= @startDate AND Date <= @endDate AND IsActive = 1
            ORDER BY Date;";
        command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

        return ReadHolidays(command);
    }

    public void InsertHoliday(HolidayRecord holiday)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Holidays (Date, Name, IsBuiltIn, IsActive)
            VALUES (@date, @name, @isBuiltIn, @isActive);";
        command.Parameters.AddWithValue("@date", holiday.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@name", holiday.Name);
        command.Parameters.AddWithValue("@isBuiltIn", holiday.IsBuiltIn ? 1 : 0);
        command.Parameters.AddWithValue("@isActive", holiday.IsActive ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public bool ExistsBuiltInForDate(DateOnly date)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Holidays WHERE Date = @date AND IsBuiltIn = 1;";
        command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
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
            holidays.Add(new HolidayRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Date = DateOnly.Parse(reader.GetString(reader.GetOrdinal("Date"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                IsBuiltIn = reader.GetInt32(reader.GetOrdinal("IsBuiltIn")) == 1,
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1
            });
        }
        return holidays;
    }
}
