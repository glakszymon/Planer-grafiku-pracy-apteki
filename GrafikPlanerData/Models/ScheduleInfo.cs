namespace GrafikPlanerData.Models;

public class ScheduleInfo
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string Name { get; set; }
    public DateTime LastModified { get; set; }

    public ScheduleInfo(int month, int year)
    {
        Month = month;
        Year = year;
        Name = $"{month:D2} - {year}";
        LastModified = DateTime.Now;
    }
}