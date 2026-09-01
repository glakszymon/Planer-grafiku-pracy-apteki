using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
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
    private ScheduleInfo? _scheduleToDelete = null;
    private HoursRecord? _editingHour = null;
    private HoursRecord? _hourToDelete = null;
    private bool _isLoadingSettings = false;

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
        SetActiveTab("grafiki");
    }

    // ==================== TABS ====================

    private void OnTabGrafikiClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveTab("grafiki");
    }

    private void OnTabPracownicyClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveTab("pracownicy");
    }

    private void OnTabUstawieniaClick(object? sender, PointerPressedEventArgs e)
    {
        LoadSettings();
        LoadHours();
        LoadHolidays();
        CheckShiftCoverage();
        SetActiveTab("ustawienia");
    }

    // ==================== SETTINGS SUB-TABS ====================

    private void OnSettingsSubTabGodzinyClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveSettingsSubTab("godziny");
    }

    private void OnSettingsSubTabSwietaClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveSettingsSubTab("swieta");
    }

    private void OnSettingsSubTabZmianyClick(object? sender, PointerPressedEventArgs e)
    {
        SetActiveSettingsSubTab("zmiany");
    }

    private void SetActiveSettingsSubTab(string subTab)
    {
        var tabs = new[] { SettingsSubTabGodziny, SettingsSubTabSwieta, SettingsSubTabZmiany };
        var pages = new[] { SettingsPageGodziny, SettingsPageSwieta, SettingsPageZmiany };
        var names = new[] { "godziny", "swieta", "zmiany" };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = names[i] == subTab;
            tabs[i].Background = new SolidColorBrush(Color.Parse(active ? "White" : "Transparent"));
            if (tabs[i].Child is TextBlock tb)
            {
                tb.Foreground = new SolidColorBrush(Color.Parse(active ? "#3A3A3A" : "#6A6A6A"));
            }
            pages[i].IsVisible = active;
        }
    }

    private void SetActiveTab(string tab)
    {
        var tabs = new[] { TabGrafiki, TabPracownicy, TabUstawienia };
        var pages = new[] { PageGrafiki, PagePracownicy, PageUstawienia };
        var names = new[] { "grafiki", "pracownicy", "ustawienia" };

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = names[i] == tab;
            tabs[i].Background = new SolidColorBrush(Color.Parse(active ? "#F8F7F5" : "#EFECEA"));
            tabs[i].BorderThickness = new Avalonia.Thickness(1, 1, 1, active ? 0 : 1);
            pages[i].IsVisible = active;
            pages[i].Opacity = active ? 1 : 0;
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

    private void OnScheduleItemClick(object? sender, PointerPressedEventArgs e)
    {
        // Don't open schedule if click came from the delete button
        if (e.Source is Avalonia.Controls.Button || 
            (e.Source is Avalonia.Visual v && v.FindAncestorOfType<Button>() != null && 
             v.FindAncestorOfType<Button>() != sender))
            return;

        if (sender is Border border && border.Tag is ScheduleInfo selectedRecord)
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
            StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#C05050"));
        }
    }

    private void OnDeleteScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ScheduleInfo schedule)
        {
            _scheduleToDelete = schedule;
            DeleteScheduleMessage.Text = $"Czy na pewno chcesz usunąć grafik \"{schedule.Name}\"? Wszystkie dane zmianowe zostaną trwale usunięte.";
            DeleteScheduleOverlay.IsVisible = true;
        }
    }

    private void OnConfirmDeleteScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (_scheduleToDelete == null) return;

        var shiftTable = new ShiftTable();
        shiftTable.StartConnectionWithDatabase();
        shiftTable.DeleteSchedule(_scheduleToDelete.Month, _scheduleToDelete.Year);

        StatusTextBlock.Text = $"Usunięto grafik: {_scheduleToDelete.Name}";
        StatusTextBlock.Foreground = new SolidColorBrush(Color.Parse("#C05050"));

        _scheduleToDelete = null;
        DeleteScheduleOverlay.IsVisible = false;
        LoadListOfSchedules();
    }

    private void OnCancelDeleteScheduleClick(object? sender, RoutedEventArgs e)
    {
        _scheduleToDelete = null;
        DeleteScheduleOverlay.IsVisible = false;
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

        // Animacja: fade out panel, załaduj dane, fade in
        DetailPanel.Opacity = 0;

        ShowDetailView(emp);
        HighlightSelectedEmployee();

        // Fade in (Transition animuje automatycznie)
        DetailPanel.Opacity = 1;
    }

    private void HighlightSelectedEmployee()
    {
        if (EmployeesList.ItemsSource == null) return;

        var panel = EmployeesList.GetVisualChildren().FirstOrDefault();
        if (panel == null) return;

        foreach (var child in panel.GetVisualChildren())
        {
            if (child is Border border && border.Tag is EmployeeRecord rec)
            {
                bool active = rec.Id == _selectedEmployee?.Id;
                border.Background = new SolidColorBrush(Color.Parse(active ? "White" : "Transparent"));
            }
        }
    }

    private void ShowDetailView(EmployeeRecord emp)
    {
        DetailEmpty.IsVisible = false;
        DetailEdit.IsVisible = false;
        DetailView.IsVisible = true;
        DeleteConfirmPanel.IsVisible = false;

        ViewAvatar.Text = emp.FirstName.Length > 0 ? emp.FirstName[0].ToString() : "?";
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
        EditSubmitBtn.Content = "Dodaj";
        ClearEmployeeForm();
        EditAvatar.Text = "?";

        DetailEmpty.IsVisible = false;
        DetailView.IsVisible = false;
        DetailEdit.IsVisible = true;
    }

    private void OnEditEmployeeClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedEmployee == null) return;

        _editingEmployee = _selectedEmployee;
        EditSubmitBtn.Content = "Zapisz zmiany";

        EmpFirstNameBox.Text = _selectedEmployee.FirstName;
        EmpLastNameBox.Text = _selectedEmployee.LastName;
        SelectComboBoxItem(EmpSpecBox, _selectedEmployee.Specialisation);
        EmpEmailBox.Text = _selectedEmployee.Email ?? "";
        EmpPhoneBox.Text = _selectedEmployee.PhoneNumber ?? "";
        EmpVacationDaysBox.Text = _selectedEmployee.VacationDays?.ToString() ?? "";
        EmpUsedVacationBox.Text = _selectedEmployee.UsedVacationDays?.ToString() ?? "";
        EmpUnusedVacationBox.Text = _selectedEmployee.UnusedVacationDaysFromLastYear?.ToString() ?? "";
        EmployeeDialogError.Text = "";
        EditAvatar.Text = _selectedEmployee.FirstName.Length > 0 ? _selectedEmployee.FirstName[0].ToString() : "?";

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
        var spec = (EmpSpecBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? "";

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
        EmpSpecBox.SelectedIndex = -1;
        EmpEmailBox.Text = "";
        EmpPhoneBox.Text = "";
        EmpVacationDaysBox.Text = "";
        EmpUsedVacationBox.Text = "";
        EmpUnusedVacationBox.Text = "";
        EmployeeDialogError.Text = "";
    }

    private void SelectComboBoxItem(ComboBox comboBox, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            comboBox.SelectedIndex = -1;
            return;
        }

        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item && item.Content?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
        comboBox.SelectedIndex = -1;
    }

    // ==================== SETTINGS ====================

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        var settings = settingsTable.GetSettings();
        if (settings == null) { _isLoadingSettings = false; return; }

        ChkMonday.IsChecked = settings.MondayOpen;
        ChkTuesday.IsChecked = settings.TuesdayOpen;
        ChkWednesday.IsChecked = settings.WednesdayOpen;
        ChkThursday.IsChecked = settings.ThursdayOpen;
        ChkFriday.IsChecked = settings.FridayOpen;
        ChkSaturday.IsChecked = settings.SaturdayOpen;
        ChkSunday.IsChecked = settings.SundayOpen;

        MondayOpenTime.SelectedTime = settings.MondayOpeningTime.ToTimeSpan();
        MondayCloseTime.SelectedTime = settings.MondayClosingTime.ToTimeSpan();
        TuesdayOpenTime.SelectedTime = settings.TuesdayOpeningTime.ToTimeSpan();
        TuesdayCloseTime.SelectedTime = settings.TuesdayClosingTime.ToTimeSpan();
        WednesdayOpenTime.SelectedTime = settings.WednesdayOpeningTime.ToTimeSpan();
        WednesdayCloseTime.SelectedTime = settings.WednesdayClosingTime.ToTimeSpan();
        ThursdayOpenTime.SelectedTime = settings.ThursdayOpeningTime.ToTimeSpan();
        ThursdayCloseTime.SelectedTime = settings.ThursdayClosingTime.ToTimeSpan();
        FridayOpenTime.SelectedTime = settings.FridayOpeningTime.ToTimeSpan();
        FridayCloseTime.SelectedTime = settings.FridayClosingTime.ToTimeSpan();
        SaturdayOpenTime.SelectedTime = settings.SaturdayOpeningTime.ToTimeSpan();
        SaturdayCloseTime.SelectedTime = settings.SaturdayClosingTime.ToTimeSpan();
        SundayOpenTime.SelectedTime = settings.SundayOpeningTime.ToTimeSpan();
        SundayCloseTime.SelectedTime = settings.SundayClosingTime.ToTimeSpan();

        AllDaysOpeningTime.SelectedTime = settings.OpeningTime.ToTimeSpan();
        AllDaysClosingTime.SelectedTime = settings.ClosingTime.ToTimeSpan();

        _isLoadingSettings = false;
    }

    private void OnSettingChanged(object? sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        AutoSaveSettings();
    }

    private void OnSettingTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        AutoSaveSettings();
    }

    private void OnApplyAllDaysHoursClick(object? sender, RoutedEventArgs e)
    {
        if (!AllDaysOpeningTime.SelectedTime.HasValue || !AllDaysClosingTime.SelectedTime.HasValue)
            return;

        _isLoadingSettings = true;
        var open = AllDaysOpeningTime.SelectedTime.Value;
        var close = AllDaysClosingTime.SelectedTime.Value;

        MondayOpenTime.SelectedTime = open;
        MondayCloseTime.SelectedTime = close;
        TuesdayOpenTime.SelectedTime = open;
        TuesdayCloseTime.SelectedTime = close;
        WednesdayOpenTime.SelectedTime = open;
        WednesdayCloseTime.SelectedTime = close;
        ThursdayOpenTime.SelectedTime = open;
        ThursdayCloseTime.SelectedTime = close;
        FridayOpenTime.SelectedTime = open;
        FridayCloseTime.SelectedTime = close;
        SaturdayOpenTime.SelectedTime = open;
        SaturdayCloseTime.SelectedTime = close;
        SundayOpenTime.SelectedTime = open;
        SundayCloseTime.SelectedTime = close;
        _isLoadingSettings = false;

        AutoSaveSettings();
    }

    private TimeOnly GetTimeFromPicker(TimePicker picker, TimeOnly fallback)
    {
        return picker.SelectedTime.HasValue 
            ? TimeOnly.FromTimeSpan(picker.SelectedTime.Value) 
            : fallback;
    }

    private void AutoSaveSettings()
    {
        var fallbackOpen = new TimeOnly(8, 0);
        var fallbackClose = new TimeOnly(22, 0);

        var record = new SettingsRecord
        {
            OpeningTime = GetTimeFromPicker(AllDaysOpeningTime, fallbackOpen),
            ClosingTime = GetTimeFromPicker(AllDaysClosingTime, fallbackClose),
            MondayOpen = ChkMonday.IsChecked == true,
            TuesdayOpen = ChkTuesday.IsChecked == true,
            WednesdayOpen = ChkWednesday.IsChecked == true,
            ThursdayOpen = ChkThursday.IsChecked == true,
            FridayOpen = ChkFriday.IsChecked == true,
            SaturdayOpen = ChkSaturday.IsChecked == true,
            SundayOpen = ChkSunday.IsChecked == true,
            MondayOpeningTime = GetTimeFromPicker(MondayOpenTime, fallbackOpen),
            MondayClosingTime = GetTimeFromPicker(MondayCloseTime, fallbackClose),
            TuesdayOpeningTime = GetTimeFromPicker(TuesdayOpenTime, fallbackOpen),
            TuesdayClosingTime = GetTimeFromPicker(TuesdayCloseTime, fallbackClose),
            WednesdayOpeningTime = GetTimeFromPicker(WednesdayOpenTime, fallbackOpen),
            WednesdayClosingTime = GetTimeFromPicker(WednesdayCloseTime, fallbackClose),
            ThursdayOpeningTime = GetTimeFromPicker(ThursdayOpenTime, fallbackOpen),
            ThursdayClosingTime = GetTimeFromPicker(ThursdayCloseTime, fallbackClose),
            FridayOpeningTime = GetTimeFromPicker(FridayOpenTime, fallbackOpen),
            FridayClosingTime = GetTimeFromPicker(FridayCloseTime, fallbackClose),
            SaturdayOpeningTime = GetTimeFromPicker(SaturdayOpenTime, fallbackOpen),
            SaturdayClosingTime = GetTimeFromPicker(SaturdayCloseTime, fallbackClose),
            SundayOpeningTime = GetTimeFromPicker(SundayOpenTime, fallbackOpen),
            SundayClosingTime = GetTimeFromPicker(SundayCloseTime, fallbackClose),
        };

        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        settingsTable.UpdateSettings(record);

        CheckShiftCoverage();
    }

    // ==================== HOLIDAYS ====================

    private void LoadHolidays()
    {
        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        PolishHolidays.SeedBuiltIn(holidaysTable);
        
        var holidays = holidaysTable.GetAllHolidays();
        HolidaysList.ItemsSource = holidays;
    }

    private void OnHolidayToggleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is HolidayRecord holiday)
        {
            var holidaysTable = new HolidaysTable();
            holidaysTable.StartConnectionWithDatabase();
            holidaysTable.UpdateHolidayActive(holiday.Id, checkBox.IsChecked == true);
        }
    }

    private void OnDeleteHolidayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is HolidayRecord holiday)
        {
            var holidaysTable = new HolidaysTable();
            holidaysTable.StartConnectionWithDatabase();
            holidaysTable.DeleteHoliday(holiday.Id);
            LoadHolidays();
        }
    }

    private void OnAddCustomHolidayClick(object? sender, RoutedEventArgs e)
    {
        HolidayErrorText.Text = "";

        if (!CustomHolidayDate.SelectedDate.HasValue)
        {
            HolidayErrorText.Text = "Wybierz datę święta.";
            return;
        }

        var selectedDate = CustomHolidayDate.SelectedDate.Value;
        int day = selectedDate.Day;
        int month = selectedDate.Month;

        var name = CustomHolidayName.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            HolidayErrorText.Text = "Podaj nazwę święta.";
            return;
        }

        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        holidaysTable.InsertHoliday(new HolidayRecord
        {
            Month = month,
            Day = day,
            Name = name,
            IsBuiltIn = false,
            IsActive = true
        });

        CustomHolidayDate.SelectedDate = null;
        CustomHolidayName.Text = "";
        AddHolidayOverlay.IsVisible = false;
        LoadHolidays();
    }

    private void OnShowAddHolidayClick(object? sender, RoutedEventArgs e)
    {
        HolidayErrorText.Text = "";
        CustomHolidayDate.SelectedDate = null;
        CustomHolidayName.Text = "";
        AddHolidayOverlay.IsVisible = true;
    }

    private void OnCancelAddHolidayClick(object? sender, RoutedEventArgs e)
    {
        AddHolidayOverlay.IsVisible = false;
    }

    // ==================== SHIFT HOURS ====================

    private void LoadHours()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        var hours = hoursTable.GetAllHours();
        HoursList.ItemsSource = hours;
        HoursEmptyState.IsVisible = hours == null || hours.Count == 0;
    }

    private void OnAddHourClick(object? sender, RoutedEventArgs e)
    {
        _editingHour = null;
        HourEditTitle.Text = "Nowa zmiana";
        HourSymbolBox.Text = "";
        HourStartTime.SelectedTime = null;
        HourEndTime.SelectedTime = null;
        HourIsVacation.IsChecked = false;
        HourVacationHint.IsVisible = false;
        HourEditError.Text = "";
        HourEditPanel.IsVisible = true;
    }

    private void OnEditHourClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HoursRecord hour)
        {
            _editingHour = hour;
            HourEditTitle.Text = "Edytuj zmianę";
            HourSymbolBox.Text = hour.Symbol;
            HourStartTime.SelectedTime = hour.StartTime.ToTimeSpan();
            HourEndTime.SelectedTime = hour.EndTime.ToTimeSpan();
            HourIsVacation.IsChecked = hour.IsVacation;
            HourVacationHint.IsVisible = hour.IsVacation;
            HourEditError.Text = "";
            HourEditPanel.IsVisible = true;
        }
    }

    private void OnHourSubmitClick(object? sender, RoutedEventArgs e)
    {
        var symbol = HourSymbolBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(symbol))
        {
            HourEditError.Text = "Symbol nie może być pusty.";
            return;
        }

        if (!HourStartTime.SelectedTime.HasValue || !HourEndTime.SelectedTime.HasValue)
        {
            HourEditError.Text = "Wybierz godzinę rozpoczęcia i zakończenia.";
            return;
        }

        var startTime = TimeOnly.FromTimeSpan(HourStartTime.SelectedTime.Value);
        var endTime = TimeOnly.FromTimeSpan(HourEndTime.SelectedTime.Value);

        if (endTime <= startTime)
        {
            HourEditError.Text = "Godzina zakończenia musi być późniejsza niż rozpoczęcia.";
            return;
        }

        var record = new HoursRecord
        {
            Symbol = symbol,
            StartTime = startTime,
            EndTime = endTime,
            IsVacation = HourIsVacation.IsChecked == true
        };

        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();

        if (_editingHour != null)
        {
            record.Id = _editingHour.Id;
            hoursTable.UpdateHour(record);
            HourStatusText.Text = $"Zaktualizowano zmianę: {symbol}";
            HourStatusText.Foreground = new SolidColorBrush(Color.Parse("#4A7C59"));
        }
        else
        {
            hoursTable.AddHours(record);
            HourStatusText.Text = $"Dodano zmianę: {symbol}";
            HourStatusText.Foreground = new SolidColorBrush(Color.Parse("#4A7C59"));
        }

        HourEditPanel.IsVisible = false;
        LoadHours();
        CheckShiftCoverage();
    }

    private void OnHourCancelClick(object? sender, RoutedEventArgs e)
    {
        HourEditPanel.IsVisible = false;
    }

    private void OnHourIsVacationChanged(object? sender, RoutedEventArgs e)
    {
        HourVacationHint.IsVisible = HourIsVacation.IsChecked == true;
    }

    private void OnDeleteHourClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HoursRecord hour)
        {
            _hourToDelete = hour;
            DeleteHourConfirmPanel.IsVisible = true;
        }
    }

    private void OnConfirmDeleteHourClick(object? sender, RoutedEventArgs e)
    {
        if (_hourToDelete == null) return;

        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        hoursTable.DeleteHour(_hourToDelete);

        HourStatusText.Text = $"Usunięto zmianę: {_hourToDelete.Symbol}";
        HourStatusText.Foreground = new SolidColorBrush(Color.Parse("#DC2626"));

        _hourToDelete = null;
        DeleteHourConfirmPanel.IsVisible = false;
        LoadHours();
        CheckShiftCoverage();
    }

    private void OnCancelDeleteHourClick(object? sender, RoutedEventArgs e)
    {
        _hourToDelete = null;
        DeleteHourConfirmPanel.IsVisible = false;
    }

    // ==================== COVERAGE CHECK ====================

    private void CheckShiftCoverage()
    {
        var analyser = new SettingsAnalisation();
        var uncoveredHours = analyser.CheckWorkshiftsHours();

        if (uncoveredHours.Count > 0)
        {
            var hoursText = string.Join(", ", uncoveredHours.Select(h => h.ToString("HH:mm")));
            CoverageWarningPanel.IsVisible = true;
            CoverageWarningDetails.Text = $"Następujące godziny nie są objęte żadną zmianą: {hoursText}. Uzupełnij definicje zmian, aby zapewnić pełne pokrycie godzin otwarcia apteki.";
        }
        else
        {
            CoverageWarningPanel.IsVisible = false;
        }
    }
}
