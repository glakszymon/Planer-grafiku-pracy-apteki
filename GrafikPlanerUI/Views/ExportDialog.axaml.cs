using Avalonia.Controls;
using Avalonia.Interactivity;
using GrafikPlanerCore.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GrafikPlanerUI.Views;

public partial class ExportDialog : Window
{
    public bool Confirmed { get; private set; }
    public bool IsExcel => RadioExcel.IsChecked == true;
    public bool IsPdf => RadioPdf.IsChecked == true;
    public bool ExportColors => ChkColors.IsChecked == true;
    public bool ExportSpecialization => ChkSpecialization.IsChecked == true;
    public bool ExportHoursSummary => ChkHoursSummary.IsChecked == true;
    public bool ExportLegend => ChkLegend.IsChecked == true;
    public List<ScheduleRow> SelectedEmployees => _employees
        .Where(e => e.IsSelected)
        .Select(e => e.Row)
        .ToList();

    private readonly List<EmployeeSelection> _employees;

    public ExportDialog(List<ScheduleRow> rows)
    {
        InitializeComponent();

        _employees = rows.Select(r => new EmployeeSelection
        {
            Name = $"{r.FirstName} {r.LastName}",
            IsSelected = true,
            Row = r
        }).ToList();

        EmployeeList.ItemsSource = _employees;
    }

    private void Export_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}

public class EmployeeSelection : INotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public ScheduleRow Row { get; set; } = null!;

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
