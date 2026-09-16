using Microsoft.Data.Sqlite;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerData;

public class DbConnectionOption
{
    protected SqliteConnection _connection;
    
    public void StartConnectionWithDatabase()
    {
        var connectionString = DatabasePath.GetConnectionString();
        
        var tempCon = new SqliteConnection(connectionString);
        tempCon.Open();
        
        _connection = tempCon;
    }

    /// <summary>
    /// Uzupełnia schemat już istniejącej tabeli o brakujące kolumny, nie usuwając danych.
    /// Definicje mają postać "Name TYPE [constraints]" i muszą być bezpieczne dla
    /// ALTER TABLE ADD COLUMN (kolumna z NOT NULL wymaga DEFAULT przy niepustej tabeli).
    /// </summary>
    protected void EnsureColumns(string tableName, params string[] columnDefinitions)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var check = _connection.CreateCommand())
        {
            check.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = check.ExecuteReader();
            while (reader.Read())
                existingColumns.Add(reader.GetString(1));
        }

        foreach (var definition in columnDefinitions)
        {
            var columnName = definition.Split(' ', 2)[0];
            if (existingColumns.Contains(columnName))
                continue;

            using var alter = _connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {definition};";
            alter.ExecuteNonQuery();
        }
    }
}