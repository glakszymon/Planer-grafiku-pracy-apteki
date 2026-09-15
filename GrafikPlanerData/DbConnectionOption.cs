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
    
    
    
}