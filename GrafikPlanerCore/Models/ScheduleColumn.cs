namespace GrafikPlanerCore.Models;

public class ScheduleColumn
{
    public int Id { get; set; }
    
    public int EmployeeId { get; set; }
    
    public DateOnly ShiftDate { get; set; } 
    public string? PoleColor { get; set; }
    public string? PoleIcon { get; set; }

    // Hour
    public int? ShiftHourId { get; set; }
    
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Symbol { get; set; } = string.Empty;
    
    
}