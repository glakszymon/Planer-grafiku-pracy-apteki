using Microsoft.Data.Sqlite;
using GrafikPlanerData.DbScripts;

namespace GrafikPlanerData;

public class DbService
{
    private SqliteConnection _connection;
    
    private void StartConnectionWithDatabase()
    {
        var connectionString = "Data Source=apteka.db";
        
        var tempCon = new SqliteConnection(connectionString);
        tempCon.Open();
        
        _connection = tempCon;
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