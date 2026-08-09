using GrafikPlanerData.Models;
using Microsoft.Data.Sqlite;    

namespace GrafikPlanerData.DbScripts;

public class EmployeeTable : DbConnectionOption
{
    
    
    public void CreateTable()
    {
        var createEmployeeTableCommand = _connection.CreateCommand();
        
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
        
        var command = _connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Employee (FirstName, LastName, Specialization, Email, PhoneNumber) 
            VALUES (@firstName, @lastName, @specialisation, @email, @phoneNumber);";

        command.Parameters.AddWithValue("@firstName", record.FirstName);
        command.Parameters.AddWithValue("@lastName", record.LastName);
        command.Parameters.AddWithValue("@specialisation", record.Specialisation);
        command.Parameters.AddWithValue("@email", (object?)record.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@phoneNumber", (object?)record.PhoneNumber ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public List<EmployeeRecord> GetAllEmployees()
    {
        var employees = new List<EmployeeRecord>();
    
        using var command = _connection.CreateCommand();
        command.CommandText = @"
        SELECT Id, FirstName, LastName, Specialization, Email, PhoneNumber 
        FROM Employee";

        using var reader = command.ExecuteReader();

        int idOrdinal = reader.GetOrdinal("Id");
        int firstNameOrdinal = reader.GetOrdinal("FirstName");
        int lastNameOrdinal = reader.GetOrdinal("LastName");
        int specOrdinal = reader.GetOrdinal("Specialization");
        int emailOrdinal = reader.GetOrdinal("Email");
        int phoneOrdinal = reader.GetOrdinal("PhoneNumber");

        while (reader.Read())
        {
            var emp = new EmployeeRecord
            {
                Id = reader.GetInt32(idOrdinal),
                FirstName = reader.GetString(firstNameOrdinal),
                LastName = reader.GetString(lastNameOrdinal),
                Specialisation = reader.IsDBNull(specOrdinal) ? string.Empty : reader.GetString(specOrdinal),
                Email = reader.IsDBNull(emailOrdinal) ? string.Empty : reader.GetString(emailOrdinal),
                PhoneNumber = reader.IsDBNull(phoneOrdinal) ? string.Empty : reader.GetString(phoneOrdinal)
            };
        
            employees.Add(emp);
        }
    
        return employees;
    }
       
}