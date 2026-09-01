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
    public int? UsedVacationDays { get; set; }
    public int? UnusedVacationDaysFromLastYear { get; set; }
    public int YearOfVacationData { get; set; }
    
    // Wymiar czasu pracy i umowa
    public EmploymentType EmploymentType { get; set; } = EmploymentType.UmowaPrace;
    public WorkTimeRate WorkTimeRate { get; set; } = WorkTimeRate.Full;
    public WorkTimeSystem WorkTimeSystem { get; set; } = WorkTimeSystem.Podstawowy;
    
    // Twarde ograniczenia prawne
    public bool ParentalProtection { get; set; } = false;
    public bool ReducedNorm { get; set; } = false;
    public bool AutoDailyRest { get; set; } = true;
}
