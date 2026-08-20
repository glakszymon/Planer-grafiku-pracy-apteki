namespace GrafikPlanerData.Models;

public class HoursRecord
{
    public int Id { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public bool IsVacation { get; set; }

}