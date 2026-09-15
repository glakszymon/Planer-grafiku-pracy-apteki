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

    /// <summary>
    /// Liczba przypisań urlopowych (IsVacation=1) w miesiącu — potrzebna do ostrzeżenia
    /// przy usuwaniu grafiku, że zwolni on dni urlopu.
    /// </summary>
    public int GetVacationCountOfMonth(int month, int year)
    {
        string monthPattern = $"{year:D4}-{month:D2}-%";

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(ShiftRecords.Id)
            FROM ShiftRecords
            INNER JOIN ShiftHours ON ShiftHours.Id = ShiftRecords.ShiftHourId
            WHERE ShiftHours.IsVacation = 1 AND ShiftRecords.ShiftDate LIKE @monthPattern";
        command.Parameters.AddWithValue("@monthPattern", monthPattern);

        var result = command.ExecuteScalar();
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

    /// <summary>
    /// Liczba przypisań urlopowych (ShiftHourId wskazujący na godzinę z IsVacation=1)
    /// na pracownika dla całego roku kalendarzowego i roku poprzedniego.
    /// </summary>
    public Dictionary<int, (int UsedInYear, int UsedPrevYear)> GetVacationUsages(int year, int prevYear)
    {
        var usages = new Dictionary<int, (int UsedInYear, int UsedPrevYear)>();

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT ShiftRecords.EmployeeId, substr(ShiftRecords.ShiftDate, 1, 4) AS ShiftYear, COUNT(*) AS Cnt
            FROM ShiftRecords
            INNER JOIN ShiftHours ON ShiftHours.Id = ShiftRecords.ShiftHourId
            WHERE ShiftHours.IsVacation = 1 AND substr(ShiftRecords.ShiftDate, 1, 4) IN (@year, @prevYear)
            GROUP BY ShiftRecords.EmployeeId, substr(ShiftRecords.ShiftDate, 1, 4);";

        command.Parameters.AddWithValue("@year", year.ToString());
        command.Parameters.AddWithValue("@prevYear", prevYear.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            int employeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId"));
            int shiftYear = int.Parse(reader.GetString(reader.GetOrdinal("ShiftYear")));
            int count = reader.GetInt32(reader.GetOrdinal("Cnt"));

            if (!usages.TryGetValue(employeeId, out var usage))
                usage = (0, 0);

            usage = shiftYear == year
                ? (count, usage.UsedPrevYear)
                : (usage.UsedInYear, count);

            usages[employeeId] = usage;
        }

        return usages;
    }

    /// <summary>
    /// Rekordy zmian w całym roku kalendarzowym z godzinami i flagami typu zmiany
    /// (urlop, L4) — do statystyk personalnych pracownika.
    /// </summary>
    public List<YearShiftStat> GetYearShiftStats(int year)
    {
        var stats = new List<YearShiftStat>();
        string yearPattern = $"{year:D4}-%";

        var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT ShiftRecords.EmployeeId, ShiftRecords.ShiftDate,
                   ShiftHours.StartTime, ShiftHours.EndTime,
                   ShiftHours.IsVacation, ShiftHours.IsSickLeave
            FROM ShiftRecords
            INNER JOIN ShiftHours ON ShiftHours.Id = ShiftRecords.ShiftHourId
            WHERE ShiftRecords.ShiftDate LIKE @yearPattern";

        command.Parameters.AddWithValue("@yearPattern", yearPattern);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var start = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("StartTime")));
            var end = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("EndTime")));

            stats.Add(new YearShiftStat
            {
                EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                ShiftDate = DateOnly.Parse(reader.GetString(reader.GetOrdinal("ShiftDate"))),
                Hours = (int)(end - start).TotalHours,
                IsVacation = reader.GetInt32(reader.GetOrdinal("IsVacation")) == 1,
                IsSickLeave = reader.GetInt32(reader.GetOrdinal("IsSickLeave")) == 1
            });
        }

        return stats;
    }

    public ShiftRecord? GetLastShiftInMonth(int month, int year, int workerId)
    {
        var command = _connection.CreateCommand();
        string monthPattern = $"{year:D4}-{month:D2}-%";
        command.CommandText = @"
            SELECT ShiftRecords.Id, ShiftRecords.EmployeeId, ShiftRecords.ShiftHourId, ShiftRecords.ShiftDate, ShiftRecords.PoleColor, ShiftRecords.PoleIcon
            FROM ShiftRecords WHERE ( ShiftDate LIKE @monthPattern ) AND ( EmployeeId LIKE @workerId)
            ORDER BY ShiftDate DESC LIMIT 1;";
        
        command.Parameters.AddWithValue("@monthPattern", monthPattern);
        command.Parameters.AddWithValue("@workerId", workerId);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var shiftRecord = new ShiftRecord();
            shiftRecord.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            shiftRecord.EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId"));
            shiftRecord.ShiftHourId = reader.IsDBNull(reader.GetOrdinal("ShiftHourId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ShiftHourId"));
            shiftRecord.ShiftDate = DateOnly.Parse(reader.GetString(reader.GetOrdinal("ShiftDate")));
            shiftRecord.PoleColor = reader.IsDBNull(reader.GetOrdinal("PoleColor")) ? null : reader.GetString(reader.GetOrdinal("PoleColor"));
            shiftRecord.PoleIcon = reader.IsDBNull(reader.GetOrdinal("PoleIcon")) ? null : reader.GetString(reader.GetOrdinal("PoleIcon"));
            
            return shiftRecord;
        }
        return null;
    }
}