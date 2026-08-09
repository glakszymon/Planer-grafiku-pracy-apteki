using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GrafikPlanerCore;
using GrafikPlanerData;
using GrafikPlanerUI.ViewModels;

namespace GrafikPlanerUI.Views;

public partial class MainWindow : Window
{
    private CoreProgram _coreProgram = new CoreProgram();
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Dobre miejsce na przypisanie ViewModelu do DataContext okna:
        DataContext = new MainViewModel();

        _coreProgram.RunInitializeDatabase();
    }

    private void OnSubmitClick(object? sender, RoutedEventArgs e)
    {
        if (MonthYearPicker.SelectedDate.HasValue)
        {
            DateTimeOffset selectedDate = MonthYearPicker.SelectedDate.Value;

            var ans = _coreProgram.CreateNewSchedule(selectedDate.Month, selectedDate.Year);

            if (ans.Status == "SUCCESS")
            {
                StatusTextBlock.Text = $"Sukces! Wybrano: {selectedDate:MMMM yyyy} i stworzono grafik na ten miesiac";
                StatusTextBlock.Foreground = Brushes.Green;
            }
            else
            {
                StatusTextBlock.Text = "Grafik juz istnieje";
                StatusTextBlock.Foreground = Brushes.Yellow;
            }
        }
        else
        {
            StatusTextBlock.Text = "Błąd: Wybierz miesiąc i rok!";
            StatusTextBlock.Foreground = Brushes.Red;
        }
    }
}