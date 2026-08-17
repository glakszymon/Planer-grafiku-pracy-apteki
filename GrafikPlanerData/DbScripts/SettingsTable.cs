using GrafikPlanerData.Models;

namespace GrafikPlanerData.DbScripts;

public class SettingsTable : DbConnectionOption
{
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
                    SundayOpen = @sundayOpen
            WHERE Id = 1;";

        // Użycie stałego formatu "HH:mm:ss" i rzutowanie bool na int (1 / 0)
        command.Parameters.AddWithValue("@openingTime", settings.OpeningTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@closingTime", settings.ClosingTime.ToString("HH:mm:ss"));
        command.Parameters.AddWithValue("@mondayOpen", settings.MondayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@tuesdayOpen", settings.TuesdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@wednesdayOpen", settings.WednesdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@thursdayOpen", settings.ThursdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@fridayOpen", settings.FridayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@saturdayOpen", settings.SaturdayOpen ? 1 : 0);
        command.Parameters.AddWithValue("@sundayOpen", settings.SundayOpen ? 1 : 0);
        
        command.ExecuteNonQuery();
    }

    public SettingsRecord? GetSettings()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Settings WHERE Id = 1;";

        using var reader = command.ExecuteReader();
    
        if (!reader.Read())
        {
            return null;
        }

        return new SettingsRecord
        {
            OpeningTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("OpeningTime"))),
            ClosingTime = TimeOnly.Parse(reader.GetString(reader.GetOrdinal("ClosingTime"))),
            MondayOpen = reader.GetInt32(reader.GetOrdinal("MondayOpen")) == 1,
            TuesdayOpen = reader.GetInt32(reader.GetOrdinal("TuesdayOpen")) == 1,
            WednesdayOpen = reader.GetInt32(reader.GetOrdinal("WednesdayOpen")) == 1,
            ThursdayOpen = reader.GetInt32(reader.GetOrdinal("ThursdayOpen")) == 1,
            FridayOpen = reader.GetInt32(reader.GetOrdinal("FridayOpen")) == 1,
            SaturdayOpen = reader.GetInt32(reader.GetOrdinal("SaturdayOpen")) == 1,
            SundayOpen = reader.GetInt32(reader.GetOrdinal("SundayOpen")) == 1
        };
    }
}