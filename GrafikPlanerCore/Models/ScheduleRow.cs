using System.ComponentModel;

namespace GrafikPlanerCore.Models;

public class ScheduleRow : INotifyPropertyChanged
{
    public const int GeneralGapRowId = -1;
    public const int PharmacistGapRowId = -2;
    
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VacationLine1)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VacationLine2)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VacationLine1Color)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VacationLine2Color)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameColor)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVacationCritical)));
            }
        }
    }
    
    public int? UnusedVacationDaysFromLastYear { get; set; }

    public string NameColor
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if ((total > 0 || unused > 0) && used > total + unused)
                return "#DC2626";
            return "#1A202C";
        }
    }

    public bool IsVacationCritical
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            return (total > 0 || unused > 0) && used > total + unused;
        }
    }

    public string VacationLine1
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if (total == 0 && unused == 0) return "";

            var maxPool = total + unused;
            var remaining = maxPool - used;

            if (used > maxPool)
                return $"Zostało: 🚨{remaining}";

            var remainingUnused = Math.Max(0, unused - used);
            var usedFromCurrent = Math.Max(0, used - unused);
            var remainingCurrent = total - usedFromCurrent;

            if (unused > 0)
                return $"Zostało: {remaining} ({remainingUnused}z+{remainingCurrent}b)";
            return $"Zostało: {remaining}";
        }
    }

    public string VacationLine2
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if (total == 0 && unused == 0) return "";

            var maxPool = total + unused;
            return $"Wykorz: {used} z {maxPool}";
        }
    }

    public string VacationLine1Color
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if (total == 0 && unused == 0) return "#7A7A7A";

            var maxPool = total + unused;
            if (used > maxPool) return "#DC2626"; // czerwony — przekroczenie
            return "#16A34A"; // zielony — zostało
        }
    }

    public string VacationLine2Color
    {
        get
        {
            var used = UsedVacationDays ?? 0;
            var total = VacationDays ?? 0;
            var unused = UnusedVacationDaysFromLastYear ?? 0;
            if (total == 0 && unused == 0) return "#7A7A7A";

            var maxPool = total + unused;
            if (used > maxPool) return "#DC2626"; // czerwony
            return "#7A7A7A"; // szary
        }
    }

    public List<ScheduleColumn> Records { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}