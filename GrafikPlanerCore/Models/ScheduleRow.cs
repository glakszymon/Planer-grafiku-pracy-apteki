using System.ComponentModel;

namespace GrafikPlanerCore.Models;

public class ScheduleRow : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialisation { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    private int _hoursSummary;
    public int HoursSummary
    {
        get => _hoursSummary;
        set
        {
            if (_hoursSummary != value)
            {
                _hoursSummary = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HoursSummary)));
            }
        }
    }

    public int? VacationDays { get; set; }
    
    private int? _usedVacationDays;
    public int? UsedVacationDays
    {
        get => _usedVacationDays;
        set
        {
            if (_usedVacationDays != value)
            {
                _usedVacationDays = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsedVacationDays)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VacationDisplay)));
            }
        }
    }
    
    public int? UnusedVacationDaysFromLastYear { get; set; }

    public string VacationDisplay
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if (total == 0 && unused == 0) return "";
            var line2 = unused > 0 ? $"Należny: {total}+{unused}zal." : $"Należny: {total} dni";
            return $"Wykorz.: {used}\n{line2}";
        }
    }

    public List<ScheduleColumn> Records { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}