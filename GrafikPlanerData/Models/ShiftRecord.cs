namespace GrafikPlanerData.Models;

public class ShiftRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int? ShiftHourId { get; set; }
    public DateOnly ShiftDate { get; set; } 
    public string? PoleColor { get; set; }
    public string? PoleIcon { get; set; }

}