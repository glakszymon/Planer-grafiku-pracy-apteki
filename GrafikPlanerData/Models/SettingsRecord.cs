namespace GrafikPlanerData.Models;

public class SettingsRecord
{
    public int Id { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public bool MondayOpen { get; set; }
    public bool TuesdayOpen { get; set; }
    public bool WednesdayOpen { get; set; }
    public bool ThursdayOpen { get; set; }
    public bool FridayOpen { get; set; }
    public bool SaturdayOpen { get; set; }
    public bool SundayOpen { get; set; }
}