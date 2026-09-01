namespace GrafikPlanerData.Models;

public class HolidayRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public bool IsActive { get; set; }
    
    /// <summary>Month for fixed holidays (1-12). Null for Easter-based holidays.</summary>
    public int? Month { get; set; }
    
    /// <summary>Day for fixed holidays (1-31). Null for Easter-based holidays.</summary>
    public int? Day { get; set; }
    
    /// <summary>Days offset from Easter Sunday. Null for fixed-date holidays.</summary>
    public int? EasterOffset { get; set; }
    
    public bool IsCustom => !IsBuiltIn;
    
    /// <summary>Display text for fixed holidays showing dd.MM, for Easter-based showing the offset description.</summary>
    public string DisplayText
    {
        get
        {
            if (Month.HasValue && Day.HasValue)
                return $"{Day:D2}.{Month:D2} — {Name}";
            if (EasterOffset.HasValue)
                return $"Wielkanoc {(EasterOffset.Value >= 0 ? "+" : "")}{EasterOffset.Value} dni — {Name}";
            return Name;
        }
    }
}
