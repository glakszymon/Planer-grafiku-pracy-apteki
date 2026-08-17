using System;
using System.Collections.Generic;
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
    private EmployeeRecord? _editingEmployee = null;
    
    public MainWindow()
    {
        InitializeComponent();
        
        Opened += (_, _) => WindowState = WindowState.Maximized;
        
        DataContext = new MainViewModel();

        _coreProgram.RunInitializeDatabase();

        LoadListOfSchedules();
        LoadEmployees();
        _coreProgram.UpdateVacationDataForAllEmployees();
    }

    // ==================== SCHEDULES ====================

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

    // ==================== EMPLOYEES ====================

    private void LoadEmployees()
    {
        var empTable = new EmployeeTable();
        empTable.StartConnectionWithDatabase();
        var employees = empTable.GetAllEmployees();
        EmployeesList.ItemsSource = employees;
        EmployeeEmptyState.IsVisible = employees == null || employees.Count == 0;
    }

    private void OnAddEmployeeClick(object? sender, RoutedEventArgs e)
    {
        _editingEmployee = null;
        EmployeeDialogTitle.Text = "Dodaj pracownika";
        EmployeeDialogSubmitBtn.Content = "Dodaj";
        ClearEmployeeForm();
        EmployeeDialogOverlay.IsVisible = true;
    }

    private void OnEditEmployeeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EmployeeRecord emp)
        {
            _editingEmployee = emp;
            EmployeeDialogTitle.Text = "Edytuj pracownika";
            EmployeeDialogSubmitBtn.Content = "Zapisz zmiany";
            EmpFirstNameBox.Text = emp.FirstName;
            EmpLastNameBox.Text = emp.LastName;
            EmpSpecBox.Text = emp.Specialisation;
            EmpEmailBox.Text = emp.Email ?? "";
            EmpPhoneBox.Text = emp.PhoneNumber ?? "";
            EmpVacationDaysBox.Text = emp.VacationDays?.ToString() ?? "";
            EmpUsedVacationBox.Text = emp.UsedVacationDays?.ToString() ?? "";
            EmpUnusedVacationBox.Text = emp.UnusedVacationDaysFromLastYear?.ToString() ?? "";
            EmployeeDialogError.Text = "";
            EmployeeDialogOverlay.IsVisible = true;
        }
    }

    private void OnDeleteEmployeeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is EmployeeRecord emp)
        {
            var empTable = new EmployeeTable();
            empTable.StartConnectionWithDatabase();
            empTable.DeleteEmployee(emp.Id);
            LoadEmployees();
            EmployeeStatusText.Text = $"Usunięto: {emp.FirstName} {emp.LastName}";
            EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#E53E3E"));
        }
    }

    private void OnEmployeeDialogSubmitClick(object? sender, RoutedEventArgs e)
    {
        var firstName = EmpFirstNameBox.Text?.Trim() ?? "";
        var lastName = EmpLastNameBox.Text?.Trim() ?? "";
        var spec = EmpSpecBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(spec))
        {
            EmployeeDialogError.Text = "Wypełnij wymagane pola (imię, nazwisko, specjalizacja).";
            return;
        }

        var record = new EmployeeRecord
        {
            FirstName = firstName,
            LastName = lastName,
            Specialisation = spec,
            Email = string.IsNullOrWhiteSpace(EmpEmailBox.Text) ? null : EmpEmailBox.Text.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(EmpPhoneBox.Text) ? null : EmpPhoneBox.Text.Trim(),
            VacationDays = int.TryParse(EmpVacationDaysBox.Text, out var vd) ? vd : null,
            UsedVacationDays = int.TryParse(EmpUsedVacationBox.Text, out var uvd) ? uvd : null,
            UnusedVacationDaysFromLastYear = int.TryParse(EmpUnusedVacationBox.Text, out var unvd) ? unvd : null,
            YearOfVacationData = DateTime.Now.Year
        };

        var empTable = new EmployeeTable();
        empTable.StartConnectionWithDatabase();

        if (_editingEmployee != null)
        {
            record.Id = _editingEmployee.Id;
            empTable.UpdateEmployee(record);
            EmployeeStatusText.Text = $"Zaktualizowano: {firstName} {lastName}";
            EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#5B7FE8"));
        }
        else
        {
            empTable.AddEmployee(record);
            EmployeeStatusText.Text = $"Dodano: {firstName} {lastName}";
            EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#38A169"));
        }

        EmployeeDialogOverlay.IsVisible = false;
        LoadEmployees();
    }

    private void OnEmployeeDialogCancelClick(object? sender, RoutedEventArgs e)
    {
        EmployeeDialogOverlay.IsVisible = false;
    }

    private void ClearEmployeeForm()
    {
        EmpFirstNameBox.Text = "";
        EmpLastNameBox.Text = "";
        EmpSpecBox.Text = "";
        EmpEmailBox.Text = "";
        EmpPhoneBox.Text = "";
        EmpVacationDaysBox.Text = "";
        EmpUsedVacationBox.Text = "";
        EmpUnusedVacationBox.Text = "";
        EmployeeDialogError.Text = "";
    }
}
