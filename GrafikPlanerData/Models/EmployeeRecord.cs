using GrafikPlanerData.Models.Enums;

namespace GrafikPlanerData.Models;

public class EmployeeRecord
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? VacationDays { get; set; }
    
    // Wymiar czasu pracy i umowa
    public EmploymentType EmploymentType { get; set; } = EmploymentType.UmowaPrace;
    public WorkTimeRate WorkTimeRate { get; set; } = WorkTimeRate.Full;
    
    // Odpoczynek dobowy
    public bool AutoDailyRest { get; set; } = true;
    
    /// <summary>Data dołączenia pracownika (ISO YYYY-MM-DD). Wymiar urlopu proporcjonalny w pierwszym roku.</summary>
    public string? JoinDate { get; set; }
}
