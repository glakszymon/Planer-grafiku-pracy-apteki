namespace GrafikPlanerCore.Models;

public class DailyRestViolation
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly Day { get; set; }
    public DateOnly PrevDay { get; set; }
    public string Message { get; set; } = string.Empty;
}