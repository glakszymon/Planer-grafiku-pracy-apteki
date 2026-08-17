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
                PhoneNumber TEXT,
                VacationDays INTIGER,
                UsedVacationDays INTIGER,
                UnusedVacationDaysFromLastYear INTIGER,
                YearOfVacationData INTIGER NOT NULL
            );";
        
        createEmployeeTableCommand.ExecuteNonQuery();
    }

    public void AddEmployee(EmployeeRecord record)
    {
        
        var command = _connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO Employee (FirstName, LastName, Specialization, Email, PhoneNumber, VacationDays, UsedVacationDays, UnusedVacationDaysFromLastYear, YearOfVacationData) 
            VALUES (@firstName, @lastName, @specialisation, @email, @phoneNumber, @vacationDays,  @usedVacationDays, @unusedVacationDaysFromLastYear, @yearOfVacationData);";

        command.Parameters.AddWithValue("@firstName", record.FirstName);
        command.Parameters.AddWithValue("@lastName", record.LastName);
        command.Parameters.AddWithValue("@specialisation", record.Specialisation);
        command.Parameters.AddWithValue("@email", (object?)record.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@phoneNumber", (object?)record.PhoneNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("@vacationDays", (object?)record.VacationDays ?? DBNull.Value);
        command.Parameters.AddWithValue("@usedVacationDays", (object?)record.UsedVacationDays ?? DBNull.Value);
        command.Parameters.AddWithValue("@unusedVacationDaysFromLastYear", (object?)record.UnusedVacationDaysFromLastYear ?? DBNull.Value);
        command.Parameters.AddWithValue("@yearOfVacationData", DateTime.Now.Year);

        command.ExecuteNonQuery();
    }

    public void UpdateEmployee(EmployeeRecord record)
    {
        var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE Employee 
            SET FirstName = @firstName, LastName = @lastName, Specialization = @specialisation, 
                Email = @email, PhoneNumber = @phoneNumber, VacationDays = @vacationDays, 
                UsedVacationDays = @usedVacationDays, UnusedVacationDaysFromLastYear = @unusedVacationDaysFromLastYear,
                YearOfVacationData =  @yearOfVacationData
            WHERE Id = @id;";

        command.Parameters.AddWithValue("@id", record.Id);
        command.Parameters.AddWithValue("@firstName", record.FirstName);
        command.Parameters.AddWithValue("@lastName", record.LastName);
        command.Parameters.AddWithValue("@specialisation", record.Specialisation);
        command.Parameters.AddWithValue("@email", (object?)record.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@phoneNumber", (object?)record.PhoneNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("@vacationDays", (object?)record.VacationDays ?? DBNull.Value);
        command.Parameters.AddWithValue("@usedVacationDays", (object?)record.UsedVacationDays ?? DBNull.Value);
        command.Parameters.AddWithValue("@unusedVacationDaysFromLastYear", (object)record.UnusedVacationDaysFromLastYear ?? DBNull.Value);
        command.Parameters.AddWithValue("@yearOfVacationData", (object)record.YearOfVacationData ?? DBNull.Value);
        
        command.ExecuteNonQuery();
    }

    public void DeleteEmployee(int id)
    {
        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM Employee WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    public List<EmployeeRecord> GetAllEmployees()
    {
        var employees = new List<EmployeeRecord>();
    
        using var command = _connection.CreateCommand();
        command.CommandText = @"
        SELECT Id, FirstName, LastName, Specialization, Email, PhoneNumber, VacationDays, UsedVacationDays, UnusedVacationDaysFromLastYear, YearOfVacationData
        FROM Employee";

        using var reader = command.ExecuteReader();

        int idOrdinal = reader.GetOrdinal("Id");
        int firstNameOrdinal = reader.GetOrdinal("FirstName");
        int lastNameOrdinal = reader.GetOrdinal("LastName");
        int specOrdinal = reader.GetOrdinal("Specialization");
        int emailOrdinal = reader.GetOrdinal("Email");
        int phoneOrdinal = reader.GetOrdinal("PhoneNumber");
        int vacationDaysOrdinal = reader.GetOrdinal("VacationDays");
        int usedVacationDaysOrdinal = reader.GetOrdinal("UsedVacationDays");
        int unusedVacationDaysOrdinal = reader.GetOrdinal("UnusedVacationDaysFromLastYear");
        int yearOfVacationDataOrdinal = reader.GetOrdinal("YearOfVacationData");

        while (reader.Read())
        {
            var emp = new EmployeeRecord
            {
                Id = reader.GetInt32(idOrdinal),
                FirstName = reader.GetString(firstNameOrdinal),
                LastName = reader.GetString(lastNameOrdinal),
                Specialisation = reader.IsDBNull(specOrdinal) ? string.Empty : reader.GetString(specOrdinal),
                Email = reader.IsDBNull(emailOrdinal) ? string.Empty : reader.GetString(emailOrdinal),
                PhoneNumber = reader.IsDBNull(phoneOrdinal) ? string.Empty : reader.GetString(phoneOrdinal),
                VacationDays = reader.IsDBNull(vacationDaysOrdinal) ? 0 : reader.GetInt32(vacationDaysOrdinal),
                UsedVacationDays = reader.IsDBNull(usedVacationDaysOrdinal) ? 0 : reader.GetInt32(usedVacationDaysOrdinal),
                UnusedVacationDaysFromLastYear = reader.IsDBNull(unusedVacationDaysOrdinal) ? 0 : reader.GetInt32(unusedVacationDaysOrdinal),
                YearOfVacationData = reader.IsDBNull(yearOfVacationDataOrdinal) ? 0 : reader.GetInt32(yearOfVacationDataOrdinal)
            };
        
            employees.Add(emp);
        }
    
        return employees;
    }

    public List<EmployeeRecord> GetEmployeesWithOutdatedVacations(int currentYear)
    {
        var employees = new List<EmployeeRecord>();
    
        using var command = _connection.CreateCommand();
        command.CommandText = @"
        SELECT Id, FirstName, LastName, Specialization, Email, PhoneNumber, VacationDays, UsedVacationDays, UnusedVacationDaysFromLastYear, YearOfVacationData
        FROM Employee
        WHERE YearOfVacationData IS NOT @yearOfVacationData ";
        
        command.Parameters.AddWithValue("@yearOfVacationData", currentYear);

        using var reader = command.ExecuteReader();

        int idOrdinal = reader.GetOrdinal("Id");
        int firstNameOrdinal = reader.GetOrdinal("FirstName");
        int lastNameOrdinal = reader.GetOrdinal("LastName");
        int specOrdinal = reader.GetOrdinal("Specialization");
        int emailOrdinal = reader.GetOrdinal("Email");
        int phoneOrdinal = reader.GetOrdinal("PhoneNumber");
        int vacationDaysOrdinal = reader.GetOrdinal("VacationDays");
        int usedVacationDaysOrdinal = reader.GetOrdinal("UsedVacationDays");
        int unusedVacationDaysOrdinal = reader.GetOrdinal("UnusedVacationDaysFromLastYear");
        int yearOfVacationDataOrdinal = reader.GetOrdinal("YearOfVacationData");

        while (reader.Read())
        {
            var emp = new EmployeeRecord
            {
                Id = reader.GetInt32(idOrdinal),
                FirstName = reader.GetString(firstNameOrdinal),
                LastName = reader.GetString(lastNameOrdinal),
                Specialisation = reader.IsDBNull(specOrdinal) ? string.Empty : reader.GetString(specOrdinal),
                Email = reader.IsDBNull(emailOrdinal) ? string.Empty : reader.GetString(emailOrdinal),
                PhoneNumber = reader.IsDBNull(phoneOrdinal) ? string.Empty : reader.GetString(phoneOrdinal),
                VacationDays = reader.IsDBNull(vacationDaysOrdinal) ? 0 : reader.GetInt32(vacationDaysOrdinal),
                UsedVacationDays = reader.IsDBNull(usedVacationDaysOrdinal) ? 0 : reader.GetInt32(usedVacationDaysOrdinal),
                UnusedVacationDaysFromLastYear = reader.IsDBNull(unusedVacationDaysOrdinal) ? 0 : reader.GetInt32(unusedVacationDaysOrdinal),
                YearOfVacationData = reader.IsDBNull(yearOfVacationDataOrdinal) ? 0 : reader.GetInt32(yearOfVacationDataOrdinal)
            };
        
            employees.Add(emp);
        }
    
        return employees;
    }
}