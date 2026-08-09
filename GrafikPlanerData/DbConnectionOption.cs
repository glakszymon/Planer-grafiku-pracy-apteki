using Microsoft.Data.Sqlite;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

namespace GrafikPlanerData;

public class DbConnectionOption
{
    protected SqliteConnection _connection;
    
    public void StartConnectionWithDatabase()
    {
        var connectionString = "Data Source=apteka.db";
        
        var tempCon = new SqliteConnection(connectionString);
        tempCon.Open();
        
        _connection = tempCon;
    }
    
    
    
}