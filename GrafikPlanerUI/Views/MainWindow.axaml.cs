using System;
using Avalonia.Controls;
using Avalonia.Input;
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
    private EmployeeRecord? _selectedEmployee = null;

    public MainWindow()
    {
        InitializeComponent();

        Opened += (_, _) => WindowState = WindowState.Maximized;

        DataContext = new MainViewModel();

        _coreProgram.RunInitializeDatabase();

        LoadListOfSchedules();
        LoadEmployees();
        _coreProgram.UpdateVacationDataForAllEmployees();

        // Domyślnie aktywna zakładka Grafiki
        SetActiveTab(isGrafiki: true);
    }

    // ==================== TABS ====================

    private void OnTabGrafikiClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveTab(isGrafiki: true);
    }

    private void OnTabPracownicyClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveTab(isGrafiki: false);
    }

    private void SetActiveTab(bool isGrafiki)
    {
        // Wizualne przełączanie zakładek klasera
        if (isGrafiki)
        {
            TabGrafiki.Background = new SolidColorBrush(Color.Parse("#F4F1EC"));
            TabGrafiki.BorderThickness = new Avalonia.Thickness(1, 1, 1, 0);
            TabPracownicy.Background = new SolidColorBrush(Color.Parse("#E5E1DB"));
            TabPracownicy.BorderThickness = new Avalonia.Thickness(1, 1, 1, 1);

            PageGrafiki.IsVisible = true;
            PageGrafiki.Opacity = 1;
            PagePracownicy.IsVisible = false;
            PagePracownicy.Opacity = 0;
        }
        else
        {
            TabPracownicy.Background = new SolidColorBrush(Color.Parse("#F4F1EC"));
            TabPracownicy.BorderThickness = new Avalonia.Thickness(1, 1, 1, 0);
            TabGrafiki.Background = new SolidColorBrush(Color.Parse("#E5E1DB"));
            TabGrafiki.BorderThickness = new Avalonia.Thickness(1, 1, 1, 1);

            PagePracownicy.IsVisible = true;
            PagePracownicy.Opacity = 1;
            PageGrafiki.IsVisible = false;
            PageGrafiki.Opacity = 0;
        }
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
            StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#4A7C59"));
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
            StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));
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

    private void OnEmployeeItemClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is EmployeeRecord emp)
        {
            SelectEmployee(emp);
        }
    }

    private void SelectEmployee(EmployeeRecord emp)
    {
        _selectedEmployee = emp;
        ShowDetailView(emp);
        HighlightSelectedEmployee();
    }

    private void HighlightSelectedEmployee()
    {
        // Iteracja po elementach listy i ustawienie wizualne "otwartej zakładki"
        if (EmployeesList.ItemsSource == null) return;

        // Wymuszamy przerysowanie przez ponowne przypisanie ItemsSource 
        // (prostsze niż iteracja po wizualnym drzewie w Avalonia)
        // Zamiast tego użyjemy stylów — wybrany element jest rozpoznawany w ShowDetailView
    }

    private void ShowDetailView(EmployeeRecord emp)
    {
        DetailEmpty.IsVisible = false;
        DetailEdit.IsVisible = false;
        DetailView.IsVisible = true;
        DeleteConfirmPanel.IsVisible = false;

        ViewFirstName.Text = emp.FirstName;
        ViewLastName.Text = emp.LastName;
        ViewSpec.Text = emp.Specialisation;
        ViewEmail.Text = string.IsNullOrEmpty(emp.Email) ? "—" : emp.Email;
        ViewPhone.Text = string.IsNullOrEmpty(emp.PhoneNumber) ? "—" : emp.PhoneNumber;
        ViewVacationDays.Text = emp.VacationDays?.ToString() ?? "—";
        ViewUsedVacation.Text = emp.UsedVacationDays?.ToString() ?? "0";
        ViewUnusedVacation.Text = emp.UnusedVacationDaysFromLastYear?.ToString() ?? "0";
    }

    private void OnAddEmployeeClick(object? sender, RoutedEventArgs e)
    {
        _editingEmployee = null;
        EditTitle.Text = "Dodaj pracownika";
        EditSubmitBtn.Content = "Dodaj";
        ClearEmployeeForm();

        DetailEmpty.IsVisible = false;
        DetailView.IsVisible = false;
        DetailEdit.IsVisible = true;
    }

    private void OnEditEmployeeClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedEmployee == null) return;

        _editingEmployee = _selectedEmployee;
        EditTitle.Text = "Edytuj pracownika";
        EditSubmitBtn.Content = "Zapisz zmiany";

        EmpFirstNameBox.Text = _selectedEmployee.FirstName;
        EmpLastNameBox.Text = _selectedEmployee.LastName;
        EmpSpecBox.Text = _selectedEmployee.Specialisation;
        EmpEmailBox.Text = _selectedEmployee.Email ?? "";
        EmpPhoneBox.Text = _selectedEmployee.PhoneNumber ?? "";
        EmpVacationDaysBox.Text = _selectedEmployee.VacationDays?.ToString() ?? "";
        EmpUsedVacationBox.Text = _selectedEmployee.UsedVacationDays?.ToString() ?? "";
        EmpUnusedVacationBox.Text = _selectedEmployee.UnusedVacationDaysFromLastYear?.ToString() ?? "";
        EmployeeDialogError.Text = "";

        DetailEmpty.IsVisible = false;
        DetailView.IsVisible = false;
        DetailEdit.IsVisible = true;
    }

    private void OnDeleteEmployeeClick(object? sender, RoutedEventArgs e)
    {
        DeleteConfirmPanel.IsVisible = true;
    }

    private void OnConfirmDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedEmployee == null) return;

        var empTable = new EmployeeTable();
        empTable.StartConnectionWithDatabase();
        empTable.DeleteEmployee(_selectedEmployee.Id);

        EmployeeStatusText.Text = $"Usunięto: {_selectedEmployee.FirstName} {_selectedEmployee.LastName}";
        EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));

        _selectedEmployee = null;
        DetailView.IsVisible = false;
        DetailEmpty.IsVisible = true;
        LoadEmployees();
    }

    private void OnCancelDeleteClick(object? sender, RoutedEventArgs e)
    {
        DeleteConfirmPanel.IsVisible = false;
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
            EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#4A7C59"));

            _selectedEmployee = record;
            LoadEmployees();
            ShowDetailView(record);
        }
        else
        {
            empTable.AddEmployee(record);
            EmployeeStatusText.Text = $"Dodano: {firstName} {lastName}";
            EmployeeStatusText.Foreground = new SolidColorBrush(Color.Parse("#4A7C59"));

            LoadEmployees();
            DetailEdit.IsVisible = false;
            DetailEmpty.IsVisible = true;
        }
    }

    private void OnEmployeeDialogCancelClick(object? sender, RoutedEventArgs e)
    {
        DetailEdit.IsVisible = false;

        if (_selectedEmployee != null)
        {
            ShowDetailView(_selectedEmployee);
        }
        else
        {
            DetailEmpty.IsVisible = true;
        }
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
