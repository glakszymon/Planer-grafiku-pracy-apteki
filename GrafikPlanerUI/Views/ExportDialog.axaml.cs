using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GrafikPlanerCore.Models;
using System;
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

        Opened += (s, e) => ClampToScreen();
    }

    public void ApplyScale(double s)
    {
        _ = s;
    }

    private void ClampToScreen()
    {
        try
        {
            var screen = Screens.ScreenFromWindow(this);
            if (screen == null) return;

            var wa = screen.WorkingArea;
            double dw = Width * screen.Scaling;
            double dh = Height * screen.Scaling;
            double maxX = wa.X + Math.Max(0, wa.Width - dw);
            double maxY = wa.Y + Math.Max(0, wa.Height - dh);
            int x = (int)Math.Clamp(Position.X, wa.X, maxX);
            int y = (int)Math.Clamp(Position.Y, wa.Y, maxY);
            Position = new PixelPoint(x, y);
        }
        catch
        {
        }
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
