using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GrafikPlanerCore;
using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;
using GrafikPlanerUI.ViewModels;

namespace GrafikPlanerUI.Views;

public partial class ScheduleTableWindow : Window
{
    private readonly ContextMenuOptions _contextMenu;
    private Border? _focusedBorder;
    private static readonly IBrush DefaultBorderBrush = Brushes.Transparent;
    private static readonly Thickness DefaultBorderThickness = new Thickness(0);
    private static readonly IBrush WeekendBackground = new SolidColorBrush(Color.Parse("#F0F0F0"));
    private static readonly Thickness FocusBorderThickness = new Thickness(3);

    public ScheduleTableWindow(ContextMenuOptions contextMenu)
    {
        _contextMenu = contextMenu;
        InitializeComponent();
    }

    public void LoadSchedule(List<ScheduleRow> scheduleRows)
    {
        if (scheduleRows == null || !scheduleRows.Any()) return;

        // 1. Pobieramy unikalne i posortowane dni
        var days = scheduleRows
            .SelectMany(r => r.Records)
            .Select(c => c.ShiftDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        // 2. Szukamy kolumny "Suma godz.", aby wstawić dni przed nią
        var lastColumn = ScheduleDataGrid.Columns.LastOrDefault();
        if (lastColumn != null)
        {
            ScheduleDataGrid.Columns.Remove(lastColumn);
        }

        // 3. Budujemy dynamicznie kolumny dla każdego dnia miesiąca
        foreach (var day in days)
        {
            var dayColumn = new DataGridTemplateColumn
            {
                Header = new TextBlock
                {
                    Text = day.ToString("dd.MM\nddd"),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),

                CellTemplate = new FuncDataTemplate<ScheduleRow>((row, namescope) =>
                {
                    var shift = row?.Records?.FirstOrDefault(r => r.ShiftDate == day);
                    bool isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;

                    // Tło komórki: kolor z bazy > szare dla weekendu > przezroczyste
                    IBrush cellBackground;
                    if (!string.IsNullOrWhiteSpace(shift?.PoleColor))
                        cellBackground = ConverterStringFromDbToColor(shift.PoleColor);
                    else if (isWeekend)
                        cellBackground = WeekendBackground;
                    else
                        cellBackground = Brushes.Transparent;

                    // Grid i zawartość komórki
                    var cellGrid = new Grid();
                    var mainText = new TextBlock
                    {
                        Text = shift?.Symbol ?? "",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    cellGrid.Children.Add(mainText);

                    if (!string.IsNullOrWhiteSpace(shift?.PoleIcon))
                    {
                        var badgeImage = LoadImageFromDbName(shift?.PoleIcon);
                        cellGrid.Children.Add(badgeImage);
                    }

                    var border = new Border
                    {
                        Background = cellBackground,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        BorderThickness = new Thickness(1, 0, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                        Padding = new Thickness(0),
                        Child = cellGrid
                    };

                    // Fokus na prawy i lewy przycisk myszy
                    border.PointerPressed += (s, e) =>
                    {
                        // Usuń fokus z poprzedniej komórki
                        if (_focusedBorder != null)
                        {
                            _focusedBorder.BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
                            _focusedBorder.BorderThickness = new Thickness(1, 0, 0, 0);
                        }

                        // Ustaw fokus na bieżącej — czarna pogrubiona ramka
                        border.BorderBrush = Brushes.Black;
                        border.BorderThickness = FocusBorderThickness;
                        _focusedBorder = border;

                        // Zaznacz wiersz w DataGrid
                        ScheduleDataGrid.SelectedItem = row;
                    };

                    // Przekazujemy cellGrid i border do metody tworzącej Flyout
                    var flyout = CreateShiftEditFlyout(row, shift, day, cellGrid, border);
                    border.ContextFlyout = flyout;

                    return border;
                })
            };

            ScheduleDataGrid.Columns.Add(dayColumn);
        }

        // 4. Przywracamy kolumnę "Suma godzin" na sam koniec
        if (lastColumn != null)
        {
            ScheduleDataGrid.Columns.Add(lastColumn);
        }

        // 5. Przypisujemy dane do tabeli
        ScheduleDataGrid.ItemsSource = scheduleRows;
    }

    private Image LoadImageFromDbName(string? iconName)
    {
        return new Image
        {
            Source = LoadBitmapFromResourceOrPath(iconName),
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 2, 2, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    private Bitmap? LoadBitmapFromResourceOrPath(string? iconFileName)
    {
        if (string.IsNullOrWhiteSpace(iconFileName)) return null;

        try
        {
            var uri = new Uri($"avares://GrafikPlanerUI/Assets/{iconFileName}.png");

            if (AssetLoader.Exists(uri))
            {
                return new Bitmap(AssetLoader.Open(uri));
            }

            if (System.IO.File.Exists(iconFileName))
            {
                return new Bitmap(iconFileName);
            }
        }
        catch
        {
            // Obsługa błędów braku pliku
        }

        return null;
    }

    private IBrush ConverterStringFromDbToColor(string? hexColor)
    {
        if (!string.IsNullOrWhiteSpace(hexColor) && Color.TryParse(hexColor, out var parsedColor))
        {
            return new SolidColorBrush(parsedColor);
        }

        return Brushes.Transparent;
    }

    private Flyout CreateShiftEditFlyout(
        ScheduleRow? row,
        ScheduleColumn? currentShift,
        DateOnly day,
        Grid cellGrid,
        Border border)
    {
        var flyout = new Flyout();

        // 1. Inicjalizacja domyślnie zaznaczonych opcji z aktualnego rekordu (currentShift)
        HoursRecord? selectedHour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == currentShift?.ShiftHourId);
        string? selectedColor = _contextMenu.Colors.FirstOrDefault(c => c == currentShift?.PoleColor);
        string? selectedIcon = _contextMenu.Icons.FirstOrDefault(i => i == currentShift?.PoleIcon);

        var hoursListBox = new ListBox
        {
            ItemsSource = _contextMenu.Hours,
            ItemTemplate = new FuncDataTemplate<HoursRecord?>((data, _) =>
                new TextBlock { Text = data != null ? $"{data.Symbol} ({data.StartTime} - {data.EndTime})" : "Brak" })
        };

        var colorsListBox = new ListBox
        {
            ItemsSource = _contextMenu.Colors,
            ItemTemplate = new FuncDataTemplate<string?>((colorHex, _) =>
            {
                if (string.IsNullOrEmpty(colorHex))
                    return new TextBlock { Text = "Brak" };

                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new Border { Width = 16, Height = 16, Background = ConverterStringFromDbToColor(colorHex), CornerRadius = new CornerRadius(3) },
                        new TextBlock { Text = colorHex, VerticalAlignment = VerticalAlignment.Center }
                    }
                };
            })
        };

        var iconsListBox = new ListBox
        {
            ItemsSource = _contextMenu.Icons,
            ItemTemplate = new FuncDataTemplate<string?>((iconName, _) =>
            {
                if (string.IsNullOrEmpty(iconName))
                    return new TextBlock { Text = "Brak", VerticalAlignment = VerticalAlignment.Center };

                var bitmap = LoadBitmapFromResourceOrPath(iconName);
                return new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new Image { Source = bitmap, Width = 20, Height = 20 },
                        new TextBlock { Text = iconName, VerticalAlignment = VerticalAlignment.Center }
                    }
                };
            })
        };

        // 2. Ustawienie zaznaczonych elementów na listach przy otwarciu
        hoursListBox.SelectedItem = selectedHour;
        colorsListBox.SelectedItem = selectedColor;
        iconsListBox.SelectedItem = selectedIcon;

        // 3. Reakcja na zmianę zaznaczenia — dynamiczna aktualizacja wyglądu komórki
        hoursListBox.SelectionChanged += (s, e) =>
        {
            var previousHour = selectedHour;
            selectedHour = hoursListBox.SelectedItem as HoursRecord;
            UpdateCellVisuals(selectedHour, selectedColor, selectedIcon, cellGrid, border, day);

            // Przeliczenie sumy godzin: odejmij stare, dodaj nowe
            if (row != null)
            {
                row.HoursSummary -= CalculateShiftHours(previousHour);
                row.HoursSummary += CalculateShiftHours(selectedHour);
            }
        };
        colorsListBox.SelectionChanged += (s, e) =>
        {
            selectedColor = colorsListBox.SelectedItem as string;
            UpdateCellVisuals(selectedHour, selectedColor, selectedIcon, cellGrid, border, day);
        };
        iconsListBox.SelectionChanged += (s, e) =>
        {
            selectedIcon = iconsListBox.SelectedItem as string;
            UpdateCellVisuals(selectedHour, selectedColor, selectedIcon, cellGrid, border, day);
        };

        // Budowanie interfejsu (Grid, kolumny)
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("140,140,140"),
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var h1 = new TextBlock { Text = "Godziny", FontWeight = FontWeight.Bold, Margin = new Thickness(5) };
        var h2 = new TextBlock { Text = "Kolor", FontWeight = FontWeight.Bold, Margin = new Thickness(5) };
        var h3 = new TextBlock { Text = "Ikona", FontWeight = FontWeight.Bold, Margin = new Thickness(5) };

        Grid.SetColumn(h1, 0); Grid.SetRow(h1, 0);
        Grid.SetColumn(h2, 1); Grid.SetRow(h2, 0);
        Grid.SetColumn(h3, 2); Grid.SetRow(h3, 0);

        Grid.SetColumn(hoursListBox, 0); Grid.SetRow(hoursListBox, 1);
        Grid.SetColumn(colorsListBox, 1); Grid.SetRow(colorsListBox, 1);
        Grid.SetColumn(iconsListBox, 2); Grid.SetRow(iconsListBox, 1);

        grid.Children.AddRange(new Control[] { h1, h2, h3, hoursListBox, colorsListBox, iconsListBox });
        flyout.Content = grid;

        // 4. Zapis przy zamknięciu (Closed)
        flyout.Closed += (sender, args) =>
        {
            if (row == null) return;

            var table = new ShiftTable();
            table.StartConnectionWithDatabase();

            if (currentShift != null && currentShift.Id > 0)
            {
                // Istniejący rekord — aktualizacja
                var recordToUpdate = new ShiftRecord
                {
                    Id = currentShift.Id,
                    EmployeeId = row.Id,
                    ShiftDate = day,
                    ShiftHourId = selectedHour?.Id,
                    PoleColor = selectedColor,
                    PoleIcon = selectedIcon
                };
                table.UpdateShiftRecord(recordToUpdate);
            }
            else
            {
                // Nowy rekord — wstawienie
                var recordToInsert = new ShiftRecord
                {
                    EmployeeId = row.Id,
                    ShiftDate = day,
                    ShiftHourId = selectedHour?.Id,
                    PoleColor = selectedColor,
                    PoleIcon = selectedIcon
                };
                table.AddShift(recordToInsert);
            }

            // Aktualizacja widoku komórki w czasie rzeczywistym
            UpdateLocalStateAndUI(row, currentShift, day, selectedHour, selectedColor, selectedIcon, cellGrid, border);
        };

        return flyout;
    }

    private int CalculateShiftHours(HoursRecord? hour)
    {
        if (hour?.StartTime == null || hour?.EndTime == null) return 0;
        return (hour.EndTime - hour.StartTime).Hours;
    }

    private void UpdateCellVisuals(
        HoursRecord? selectedHour,
        string? selectedColor,
        string? selectedIcon,
        Grid cellGrid,
        Border border,
        DateOnly day)
    {
        if (!string.IsNullOrWhiteSpace(selectedColor))
            border.Background = ConverterStringFromDbToColor(selectedColor);
        else if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
            border.Background = WeekendBackground;
        else
            border.Background = Brushes.Transparent;

        cellGrid.Children.Clear();

        var mainText = new TextBlock
        {
            Text = selectedHour?.Symbol ?? "",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        cellGrid.Children.Add(mainText);

        if (!string.IsNullOrWhiteSpace(selectedIcon))
        {
            var badgeImage = LoadImageFromDbName(selectedIcon);
            cellGrid.Children.Add(badgeImage);
        }
    }

    private void UpdateLocalStateAndUI(
        ScheduleRow row,
        ScheduleColumn? currentShift,
        DateOnly day,
        HoursRecord? selectedHour,
        string? selectedColor,
        string? selectedIcon,
        Grid cellGrid,
        Border border)
    {
        if (currentShift == null)
        {
            currentShift = new ScheduleColumn
            {
                ShiftDate = day
            };
            row.Records.Add(currentShift);
        }

        // Aktualizacja modelu w pamięci
        currentShift.ShiftHourId = selectedHour?.Id;
        currentShift.Symbol = selectedHour?.Symbol ?? "";
        currentShift.PoleColor = selectedColor;
        currentShift.PoleIcon = selectedIcon;

        // Przerysowanie komórki
        UpdateCellVisuals(selectedHour, selectedColor, selectedIcon, cellGrid, border, day);
    }
}