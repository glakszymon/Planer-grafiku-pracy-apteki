using GrafikPlanerCore.Models;
using GrafikPlanerCore.Models.Responses;
using GrafikPlanerCore.ScheduleScripts;
using GrafikPlanerData;

namespace GrafikPlanerCore;

public class CoreProgram
{
    public void RunInitializeDatabase()
    {
        var dbInit = new DbInitialization();
        dbInit.InitializeDatabase();
    }

    public Response CreateNewSchedule(int month, int year)
    {
        var scheduleCreator = new ScheduleCreator();

        if (scheduleCreator.ScheduleIsExisting(month, year))
        {
            return new Response("ERROR", "Schedule already exists");
        }
        
        scheduleCreator.GenerateRecords(month, year);
        scheduleCreator.SendScheduleToDatabase();
        
        return new Response("SUCCESS", "Schedule created");
    }
    
    public ResponseOpenSchedule OpenSchedule(int month, int year)
    {
        var scheduleReader = new ScheduleReader();
        if (!scheduleReader.SheduleExistInDb(month, year))
        {
            return new ResponseOpenSchedule("ERROR", "Schedule does not exist");
        }
        
        scheduleReader.GetDataFromDb(month, year);
        scheduleReader.TransformDataToTableStructure();

        return new ResponseOpenSchedule("SUCCESS", "Schedule opened",  scheduleReader.FinalSchedule);
    }
    
    
    
}