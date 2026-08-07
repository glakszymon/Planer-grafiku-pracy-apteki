using Microsoft.Data.Sqlite;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerData;

public class DbService
{
    private SqliteConnection _connection;
    
    private SqliteConnection StartConnectionWithDatabase()
    {
        var connectionString = "Data Source=apteka.db";
        
        var tempCon = new SqliteConnection(connectionString);
        tempCon.Open();
        
        _connection = tempCon;
        
        return _connection;
    }
    
    public void InitializeDatabase()
    {
        var hoursTable = new HoursTable(_connection);
        hoursTable.CreateTable();
        
        var employeeTable = new EmployeeTable(_connection);
        employeeTable.CreateTable();
        
        var shiftTable = new ShiftTable(_connection);
        shiftTable.CreateTable();
    }
    
}