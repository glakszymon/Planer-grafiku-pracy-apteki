using GrafikPlanerData.Models;
using Microsoft.Data.Sqlite;    

namespace GrafikPlanerData.DbScripts;

public class EmployeeTable
{
    SqliteConnection  _activeConnection;
    
    public EmployeeTable(SqliteConnection connection)
    {
        _activeConnection = connection;
    }
    
    public void CreateTable()
    {
        var createEmployeeTableCommand = _activeConnection.CreateCommand();
        
        createEmployeeTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS Employee (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Specialization TEXT NOT NULL,
                Email TEXT,
                PhoneNumber TEXT
            );";
        
        createEmployeeTableCommand.ExecuteNonQuery();
    }

    public void AddEmployee(EmployeeRecord record)
    {
        var createEmployeeTableCommand = _activeConnection.CreateCommand();
        
        var command = _activeConnection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Employee (FirstName, LastName, Specialization, Email, PhoneNumber) 
            VALUES (@firstName, @lastName, @specialisation, @email, @phoneNumber);";

        command.Parameters.AddWithValue("@firstName", record.FirstName);
        command.Parameters.AddWithValue("@lastName", record.LastName);
        command.Parameters.AddWithValue("@specialisation", record.Specialization);
        command.Parameters.AddWithValue("@email", (object?)record.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@phoneNumber", (object?)record.PhoneNumber ?? DBNull.Value);

        command.ExecuteNonQuery();
    }
    
}