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

    public List<ScheduleColumn> Records { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}