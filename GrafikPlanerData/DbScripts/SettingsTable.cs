using GrafikPlanerData.Models;

namespace GrafikPlanerData.DbScripts;

public class SettingsTable : DbConnectionOption
{
    private static readonly string[] DayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    public void CreateTable()
    {
        using var createHoursTableCommand = _connection.CreateCommand();
        
        createHoursTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Settings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OpeningTime TEXT NOT NULL,
                ClosingTime TEXT NOT NULL,
                MondayOpen INTEGER NOT NULL DEFAULT 1 CHECK (MondayOpen IN (0, 1)),
                TuesdayOpen INTEGER NOT NULL DEFAULT 1 CHECK (TuesdayOpen IN (0, 1)),
                WednesdayOpen INTEGER NOT NULL DEFAULT 1 CHECK (WednesdayOpen IN (0, 1)),
                ThursdayOpen INTEGER NOT NULL DEFAULT 1 CHECK (ThursdayOpen IN (0, 1)),
                FridayOpen INTEGER NOT NULL DEFAULT 1 CHECK (FridayOpen IN (0, 1)),
                SaturdayOpen INTEGER NOT NULL DEFAULT 0 CHECK (SaturdayOpen IN (0, 1)),
                SundayOpen INTEGER NOT NULL DEFAULT 0 CHECK (SundayOpen IN (0, 1))
            );";
        
        createHoursTableCommand.ExecuteNonQuery();
    }

    public void InitializeSettings()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT OR IGNORE INTO Settings (Id, OpeningTime, ClosingTime, MondayOpen, TuesdayOpen, WednesdayOpen, ThursdayOpen, FridayOpen, SaturdayOpen, SundayOpen) 
                VALUES (1,'08:00:00', '22:00:00', 1, 1, 1, 1, 1, 1, 1);";
        
        command.ExecuteNonQuery();
    }

    public void UpdateSettings(SettingsRecord settings)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE Settings SET
                    OpeningTime = @openingTime,
                    ClosingTime = @closingTime,
                    MondayOpen = @mondayOpen,
                    TuesdayOpen = @tuesdayOpen,
                    WednesdayOpen = @wednesdayOpen,
                    ThursdayOpen = @thursdayOpen,
                    FridayOpen = @fridayOpen,
                    SaturdayOpen = @saturdayOpen,
                    SundayOpen = @sundayOpen,
                    MondayOpeningTime = @monOpen, MondayClosingTime = @monClose,
                    TuesdayOpeningTime = @tueOpen, TuesdayClosingTime = @tueClose,
                    WednesdayOpeningTime = @wedOpen, WednesdayClosingTime = @wedClose,
                    ThursdayOpeningTime = @thuOpen, ThursdayClosingTime = @thuClose,
                    FridayOpeningTime = @friOpen, FridayClosingTime = @friClose,
                    SaturdayOpeningTime = @satOpen, SaturdayClosingTime = @satClose,
                    SundayOpeningTime = @sunOpen, SundayClosingTime = @sunClose
            WHERE Id = 1;";

        command.Parameters.AddWithValue("@openingTime", settings.OpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@closingTime", settings.ClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@mondayOpen", settings.MondayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@tuesdayOpen", settings.TuesdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@wednesdayOpen", settings.WednesdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@thursdayOpen", settings.ThursdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@fridayOpen", settings.FridayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@saturdayOpen", settings.SaturdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@sundayOpen", settings.SundayOpen ? 1 : 0);

        command.Parameters.AddWithValue("@monOpen", settings.MondayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@monClose", settings.MondayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@tueOpen", settings.TuesdayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@tueClose", settings.TuesdayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@wedOpen", settings.WednesdayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@wedClose", settings.WednesdayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@thuOpen", settings.ThursdayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@thuClose", settings.ThursdayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@friOpen", settings.FridayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@friClose", settings.FridayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@satOpen", settings.SaturdayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@satClose", settings.SaturdayClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@sunOpen", settings.SundayOpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@sunClose", settings.SundayClosingTime.ToString("HH:mm:ss"));
        
        command.ExecuteNonQuery();
    }

    public SettingsRecord GetSettings()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Settings WHERE Id = 1;";

        using var reader = command.ExecuteReader();
    
        if (!reader.Read())
        {
            return null;
        }

        var openingTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("OpeningTime")));
        var closingTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("ClosingTime")));

        return new SettingsRecord
        {
            OpeningTime = openingTime,
            ClosingTime = closingTime,
            MondayOpen = reader.GetInt32(reader.GetOrdinal("MondayOpen")) == 1,
            TuesdayOpen = reader.GetInt32(reader.GetOrdinal("TuesdayOpen")) == 1,
            WednesdayOpen = reader.GetInt32(reader.GetOrdinal("WednesdayOpen")) == 1,
            ThursdayOpen = reader.GetInt32(reader.GetOrdinal("ThursdayOpen")) == 1,
            FridayOpen = reader.GetInt32(reader.GetOrdinal("FridayOpen")) == 1,
            SaturdayOpen = reader.GetInt32(reader.GetOrdinal("SaturdayOpen")) == 1,
            SundayOpen = reader.GetInt32(reader.GetOrdinal("SundayOpen")) == 1,
            MondayOpeningTime = ReadTimeOrDefault(reader, "MondayOpeningTime", openingTime),
            MondayClosingTime = ReadTimeOrDefault(reader, "MondayClosingTime", closingTime),
            TuesdayOpeningTime = ReadTimeOrDefault(reader, "TuesdayOpeningTime", openingTime),
            TuesdayClosingTime = ReadTimeOrDefault(reader, "TuesdayClosingTime", closingTime),
            WednesdayOpeningTime = ReadTimeOrDefault(reader, "WednesdayOpeningTime", openingTime),
            WednesdayClosingTime = ReadTimeOrDefault(reader, "WednesdayClosingTime", closingTime),
            ThursdayOpeningTime = ReadTimeOrDefault(reader, "ThursdayOpeningTime", openingTime),
            ThursdayClosingTime = ReadTimeOrDefault(reader, "ThursdayClosingTime", closingTime),
            FridayOpeningTime = ReadTimeOrDefault(reader, "FridayOpeningTime", openingTime),
            FridayClosingTime = ReadTimeOrDefault(reader, "FridayClosingTime", closingTime),
            SaturdayOpeningTime = ReadTimeOrDefault(reader, "SaturdayOpeningTime", openingTime),
            SaturdayClosingTime = ReadTimeOrDefault(reader, "SaturdayClosingTime", closingTime),
            SundayOpeningTime = ReadTimeOrDefault(reader, "SundayOpeningTime", openingTime),
            SundayClosingTime = ReadTimeOrDefault(reader, "SundayClosingTime", closingTime),
        };
    }

    private static TimeOnly ReadTimeOrDefault(Microsoft.Data.Sqlite.SqliteDataReader reader, string column, TimeOnly defaultValue)
    {
        try
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal)) return defaultValue;
            return TimeOnly.Parse(reader.GetString(ordinal));
        }
        catch
        {
            return defaultValue;
        }
    }
}
