using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GrafikPlanerCore;
using GrafikPlanerCore.ScheduleScripts;
using GrafikPlanerData;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;
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

        LoadListOfSchedules();
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

    private void OnSubmitClickShow(object? sender, RoutedEventArgs e)
    {
        // Poprawiono: pobieranie daty z właściwego pickera MonthYearPickerShow
        if (MonthYearPickerShow.SelectedDate.HasValue)
        {
            DateTimeOffset selectedDate = MonthYearPickerShow.SelectedDate.Value;
        
            // 1. Pobieramy dane z logiki biznesowej
            var dataForTable = _coreProgram.OpenSchedule(selectedDate.Month, selectedDate.Year);

            if (dataForTable.Status == "SUCCESS")
            {

                var contextMenu = _coreProgram.CreateContextMenu();
                // 2. Tworzymy instancję drugiego okna
                var tableWindow = new ScheduleTableWindow(contextMenu);

                // 3. Ładujemy pobrane dane do tabeli w nowym oknie
                tableWindow.LoadSchedule(dataForTable.Data);

                // 4. Otwieramy nowe okno
                tableWindow.Show();

                // 5. (Opcjonalnie) Zamykamy lub ukrywamy główne okno:
                this.Close(); // Zamknie MainWindow całkowicie
                // lub
                // this.Hide();  // Tylko ukryje MainWindow
            }
            else
            {
                StatusTextBlockShow.Text = "Nie istnieje grafik na dany miesiac";
                StatusTextBlockShow.Foreground =  Brushes.Yellow;
            }

        }
        else
        {
            StatusTextBlockShow.Text = "Błąd: Wybierz miesiąc i rok!";
            StatusTextBlockShow.Foreground = Brushes.Red;
        }
    }

    private void LoadListOfSchedules()
    {
        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        var schedules = shiftTable.GetAllSchedulesDates();
        ItemsList.ItemsSource = schedules;
    }
    
    private void OnItemButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ScheduleInfo selectedRecord)
        {
            int month  = selectedRecord.Month;
            int year = selectedRecord.Year;

            // System.Diagnostics.Debug.WriteLine($"Kliknięto rekord ID: {id}, Nazwa: {name}");
            DateTime selectedDate = new  DateTime(year, month, 1);
        
            // 1. Pobieramy dane z logiki biznesowej
            var dataForTable = _coreProgram.OpenSchedule(selectedDate.Month, selectedDate.Year);

            if (dataForTable.Status == "SUCCESS")
            {

                var contextMenu = _coreProgram.CreateContextMenu();
                // 2. Tworzymy instancję drugiego okna
                var tableWindow = new ScheduleTableWindow(contextMenu);

                // 3. Ładujemy pobrane dane do tabeli w nowym oknie
                tableWindow.LoadSchedule(dataForTable.Data);

                // 4. Otwieramy nowe okno
                tableWindow.Show();

                // 5. (Opcjonalnie) Zamykamy lub ukrywamy główne okno:
                this.Close(); // Zamknie MainWindow całkowicie
                // lub
                // this.Hide();  // Tylko ukryje MainWindow
            }
            else
            {
                StatusTextBlockShow.Text = "Nie istnieje grafik na dany miesiac";
                StatusTextBlockShow.Foreground =  Brushes.Yellow;
            }
            
        }
    }
}