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

    public List<EmployeeRecord> GetAllEmployees()
    {
        var employees = new List<EmployeeRecord>();
        
        var command = _activeConnection.CreateCommand();
        command.CommandText = @"
            SELECT Id, FirstName, LastName, Specialisation, Email, PhoneNumber FROM Employee";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var emp = new EmployeeRecord();
            emp.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            emp.FirstName = reader.GetString(reader.GetOrdinal("FirstName"));
            emp.LastName = reader.GetString(reader.GetOrdinal("LastName"));
            emp.Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email"));
            emp.PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber"));
            
            employees.Add(emp);
        }
        
        return employees;
    }
       
}