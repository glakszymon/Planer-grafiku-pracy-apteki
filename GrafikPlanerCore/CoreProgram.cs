using GrafikPlanerCore.Models;
using GrafikPlanerCore.Models.Responses;
using GrafikPlanerCore.ScheduleScripts;
using GrafikPlanerData;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;

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

    public ContextMenuOptions CreateContextMenu()
    {
        var contextMenuOptions = new ContextMenuOptions();
        contextMenuOptions.FillColors();
        contextMenuOptions.FillHours();
        contextMenuOptions.FillIcons();
        
        return contextMenuOptions;
    }

    public void UpdateVacationDataForAllEmployees()
    {
        int currentYear = DateTime.Now.Year;
        
        var employeeTable =  new EmployeeTable();
        employeeTable.StartConnectionWithDatabase();

        var employees = employeeTable.GetAllEmployees();
        
        bool DataWasChanged = false;

        foreach (var employee in employees)
        {
            var worker = employee;
            if (currentYear != worker.YearOfVacationData)
            {
                 worker = StartNewYearCalculations(worker);
                 DataWasChanged = true;
            }

            if (worker.UnusedVacationDaysFromLastYear != 0 && DateTime.Now.Month > 9)
            {
                worker = LostLastYearVacations(worker);
                DataWasChanged = true;
            }

            if (DataWasChanged)
            {
                employeeTable.UpdateEmployee(worker);
            }
        }
    }

    private EmployeeRecord StartNewYearCalculations(EmployeeRecord employeeRecord)
    {
        var worker = employeeRecord;

        worker.UnusedVacationDaysFromLastYear = worker.VacationDays - worker.UsedVacationDays;
        worker.UsedVacationDays = 0;
        worker.YearOfVacationData = DateTime.Now.Year;
        
        return worker;
    }

    private EmployeeRecord LostLastYearVacations(EmployeeRecord employeeRecord)
    {
        var worker = employeeRecord;

        worker.UnusedVacationDaysFromLastYear = 0;
        return worker;
    }
    
    
}