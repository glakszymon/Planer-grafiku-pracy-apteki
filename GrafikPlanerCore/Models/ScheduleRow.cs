namespace GrafikPlanerCore.Models;

public class ScheduleRow
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    
    public int HoursSummary {get ; set;}
    public List<ScheduleColumn> Records { get; set; }
    
}