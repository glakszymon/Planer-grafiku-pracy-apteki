namespace GrafikPlanerData.Models;

public class HolidayRecord
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; }
    
    public string DisplayText => $"{Date:dd.MM.yyyy} — {Name}";
    public bool IsCustom => !IsBuiltIn;
}
