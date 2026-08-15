using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using GrafikPlanerCore;
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
        
        Opened += (_, _) => WindowState = WindowState.Maximized;
        
        DataContext = new MainViewModel();

        _coreProgram.RunInitializeDatabase();

        LoadListOfSchedules();
    }

    private void OnAddNewScheduleClick(object? sender, RoutedEventArgs e)
    {
        DialogErrorText.Text = "";
        DialogDatePicker.SelectedDate = null;
        DialogOverlay.IsVisible = true;
    }

    private void OnDialogSubmitClick(object? sender, RoutedEventArgs e)
    {
        if (!DialogDatePicker.SelectedDate.HasValue)
        {
            DialogErrorText.Text = "Wybierz miesiąc i rok!";
            return;
        }

        DateTimeOffset selectedDate = DialogDatePicker.SelectedDate.Value;
        var ans = _coreProgram.CreateNewSchedule(selectedDate.Month, selectedDate.Year);

        if (ans.Status == "SUCCESS")
        {
            DialogOverlay.IsVisible = false;
            LoadListOfSchedules();
            StatusTextBlock.Text = $"Utworzono grafik: {selectedDate:MMMM yyyy}";
            StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#38A169"));
        }
        else
        {
            DialogErrorText.Text = "Grafik na ten miesiąc już istnieje.";
        }
    }

    private void OnDialogCancelClick(object? sender, RoutedEventArgs e)
    {
        DialogOverlay.IsVisible = false;
    }

    private void LoadListOfSchedules()
    {
        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        var schedules = shiftTable.GetAllSchedulesDates();
        ItemsList.ItemsSource = schedules;
        EmptyStateText.IsVisible = schedules == null || schedules.Count == 0;
    }
    
    private void OnItemButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ScheduleInfo selectedRecord)
        {
            OpenSchedule(selectedRecord.Month, selectedRecord.Year);
        }
    }

    private void OpenSchedule(int month, int year)
    {
        var dataForTable = _coreProgram.OpenSchedule(month, year);

        if (dataForTable.Status == "SUCCESS")
        {
            var contextMenu = _coreProgram.CreateContextMenu();
            var tableWindow = new ScheduleTableWindow(contextMenu);
            tableWindow.LoadSchedule(dataForTable.Data);
            tableWindow.Show();
            this.Close();
        }
        else
        {
            StatusTextBlock.Text = "Nie udało się otworzyć grafiku.";
            StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#E53E3E"));
        }
    }
}
