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
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GrafikPlanerCore;
using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;
using GrafikPlanerUI.Services;
using GrafikPlanerUI.ViewModels;

namespace GrafikPlanerUI.Views;

public partial class ScheduleTableWindow : Window
{
    private readonly ContextMenuOptions _contextMenu;
    private List<ScheduleRow> _scheduleRows = new();
    private static readonly IBrush WeekendBackground = new SolidColorBrush(Color.Parse("#E2E8F0"));
    private static readonly IBrush ClosedDayBackground = new SolidColorBrush(Color.Parse("#E8EAED"));
    private static readonly IBrush ClosedDayForeground = new SolidColorBrush(Color.Parse("#A0A4AA"));
    private static readonly Thickness FocusBorderThickness = new Thickness(3);
    private static readonly IBrush SelectionBorderBrush = new SolidColorBrush(Color.Parse("#3182CE"));
    private static readonly IBrush GapRowBackground = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush GapHeaderForeground = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush PharmacistGapRowBackground = new SolidColorBrush(Color.Parse("#FFF3E0"));
    private static readonly IBrush PharmacistGapForeground = new SolidColorBrush(Color.Parse("#E65100"));
    private static readonly IBrush GapRowSeparator = new SolidColorBrush(Color.Parse("#9E9E9E"));
    private static readonly IBrush GapRowLabelBackground = new SolidColorBrush(Color.Parse("#F5F5F5"));
    private static readonly IBrush CheckMarkForeground = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush VacationCriticalBackground = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush HolidayBackground = new SolidColorBrush(Color.Parse("#F0F0EE"));
    private static readonly IBrush HolidayForeground = new SolidColorBrush(Color.Parse("#B0ADA8"));

    // Gap indicator state
    private Dictionary<DateOnly, string> _gapCache = new();
    private Dictionary<DateOnly, string> _pharmacistGapCache = new();
    private readonly ScheduleAnalisation _analiser = new();
    private ScheduleRow? _gapRow;
    private ScheduleRow? _pharmacistGapRow;
    private readonly Dictionary<DateOnly, TextBlock> _headerTextBlocks = new();
    private readonly Dictionary<DateOnly, TextBlock> _gapCellTextBlocks = new();
    private readonly Dictionary<DateOnly, TextBlock> _pharmacistGapCellTextBlocks = new();
    private HashSet<DayOfWeek> _closedDays = new();
    private HashSet<DateOnly> _holidayDates = new();
    private DataGridRow? _gapDataGridRow;
    private DataGridRow? _pharmacistGapDataGridRow;

    // Multi-select state
    private bool _isDragging;
    private readonly List<CellInfo> _selectedCells = new();

    private class CellInfo
    {
        public Border Border { get; set; } = null!;
        public Grid CellGrid { get; set; } = null!;
        public ScheduleRow Row { get; set; } = null!;
        public ScheduleColumn? Shift { get; set; }
        public DateOnly Day { get; set; }
    }

    // Registry by (Row, Day) to survive template re-creation
    private readonly Dictionary<(ScheduleRow, DateOnly), CellInfo> _cellsByKey = new();
    // Registry by Border for hit-testing during drag
    private readonly Dictionary<Border, CellInfo> _cellRegistry = new();

    public ScheduleTableWindow(ContextMenuOptions contextMenu)
    {
        _contextMenu = contextMenu;
        InitializeComponent();

        WindowState = WindowState.Maximized;
        Activated += OnFirstActivated;

        // Style gap rows with separator and distinct background
        ScheduleDataGrid.LoadingRow += OnDataGridLoadingRow;

        // Global pointer events for drag selection
        PointerMoved += OnGlobalPointerMoved;
        PointerReleased += OnGlobalPointerReleased;

        LoadLegend();
    }

    private void LoadLegend()
    {
        var hoursTable = new HoursTable();
        hoursTable.StartConnectionWithDatabase();
        var hours = hoursTable.GetAllHours();

        var legendData = hours.Select(h => new LegendItem
        {
            Symbol = h.Symbol,
            Description = h.IsVacation 
                ? $"urlop {(int)(h.EndTime - h.StartTime).TotalHours} godzinny" 
                : $"{h.StartTime:HH:mm} – {h.EndTime:HH:mm}"
        }).ToList();

        LegendItems.ItemsSource = legendData;
        LegendPanel.IsVisible = legendData.Count > 0;
    }

    private void LegendToggle_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var collapsed = LegendContent.IsVisible;
        LegendContent.IsVisible = !collapsed;
        LegendToggleButton.Content = collapsed ? "+" : "\u2212";

        if (collapsed)
        {
            LegendToggleButton.Margin = new Thickness(0, 0, -40, 0);
            LegendToggleButton.FontSize = 18;
            LegendToggleButton.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }
        else
        {
            LegendToggleButton.Margin = new Thickness(0, 0, -12, 0);
            LegendToggleButton.FontSize = 14;
            LegendToggleButton.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }
    }

    private void BackButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
    }

    private async void ExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_scheduleRows.Count == 0) return;

        var dialog = new ExportDialog(_scheduleRows);
        await dialog.ShowDialog(this);

        if (!dialog.Confirmed) return;

        var selectedRows = dialog.SelectedEmployees;
        if (selectedRows.Count == 0) return;

        var exportService = new ScheduleExportService(_contextMenu.Hours.Where(h => h != null).Cast<HoursRecord>().ToList());

        if (dialog.IsExcel)
        {
            var saveDialog = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Zapisz jako Excel",
                DefaultExtension = "xlsx",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } }
                },
                SuggestedFileName = "grafik.xlsx"
            });

            if (saveDialog != null)
            {
                exportService.ExportToExcel(saveDialog.Path.LocalPath, selectedRows, dialog.ExportColors, dialog.ExportLegend);
            }
        }
        else
        {
            var saveDialog = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Zapisz jako PDF",
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                },
                SuggestedFileName = "grafik.pdf"
            });

            if (saveDialog != null)
            {
                exportService.ExportToPdf(saveDialog.Path.LocalPath, selectedRows, dialog.ExportColors, dialog.ExportLegend);
            }
        }
    }

    private void OnFirstActivated(object? sender, EventArgs e)
    {
        Activated -= OnFirstActivated;
        Dispatcher.UIThread.Post(() =>
        {
            WindowState = WindowState.Maximized;
        }, DispatcherPriority.Background);
    }

    public void LoadSchedule(List<ScheduleRow> scheduleRows)
    {
        if (scheduleRows == null || !scheduleRows.Any()) return;
        _scheduleRows = scheduleRows;

        var firstDate = scheduleRows
            .SelectMany(r => r.Records)
            .Select(c => c.ShiftDate)
            .Where(d => d != default)
            .OrderBy(d => d)
            .FirstOrDefault();
        if (firstDate != default)
        {
            ScheduleTitleText.Text = $"{firstDate:MMMM yyyy}";
        }

        // Load closed days from settings
        _closedDays = LoadClosedDays();
        _holidayDates = LoadHolidayDates(scheduleRows);

        if (scheduleRows[0].Records?.Count > 0)
        {
            var firstRec = scheduleRows[0].Records[0];
        }

        var days = scheduleRows
            .SelectMany(r => r.Records)
            .Select(c => c.ShiftDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        foreach (var day in days)
        {
            var dayColumn = new DataGridTemplateColumn
            {
                Header = CreateHeaderTextBlock(day),
                Width = new DataGridLength(CalculateDayColumnWidth(days.Count), DataGridLengthUnitType.Pixel),

                CellTemplate = new FuncDataTemplate<ScheduleRow>((row, namescope) =>
                {
                    if (row == null) return new Border();

                    // Gap row - special rendering
                    if (row.Id == ScheduleRow.GeneralGapRowId)
                    {
                        return CreateGapCell(day);
                    }
                    if (row.Id == ScheduleRow.PharmacistGapRowId)
                    {
                        return CreatePharmacistGapCell(day);
                    }

                    bool isClosedDay = _closedDays.Contains(day.DayOfWeek);
                    bool isHoliday = _holidayDates.Contains(day);

                    // Closed day or holiday - non-interactive cell
                    if (isClosedDay || isHoliday)
                    {
                        var bg = isHoliday ? HolidayBackground : ClosedDayBackground;
                        var fg = isHoliday ? HolidayForeground : ClosedDayForeground;
                        return new Border
                        {
                            Background = bg,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            BorderThickness = new Thickness(1, 0, 0, 0),
                            BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
                            Child = new TextBlock
                            {
                                Text = isHoliday ? "Święto" : "—",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = fg,
                                FontSize = isHoliday ? 10 : 14,
                                FontStyle = isHoliday ? FontStyle.Italic : FontStyle.Normal
                            }
                        };
                    }

                    var shift = row.Records?.FirstOrDefault(r => r.ShiftDate == day);
                    bool isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;


                    IBrush cellBackground;
                    if (!string.IsNullOrWhiteSpace(shift?.PoleColor))
                        cellBackground = ConverterStringFromDbToColor(shift.PoleColor);
                    else if (isWeekend)
                        cellBackground = WeekendBackground;
                    else
                        cellBackground = Brushes.Transparent;

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

                    // Register cell - update references if template is re-created
                    var key = (row, day);
                    CellInfo cellInfo;
                    if (_cellsByKey.TryGetValue(key, out var existingCell))
                    {
                        // Template was re-created, update references
                        _cellRegistry.Remove(existingCell.Border);
                        existingCell.Border = border;
                        existingCell.CellGrid = cellGrid;
                        // Update shift reference - it may have been created by EnsureShiftExists
                        if (existingCell.Shift == null && shift != null)
                            existingCell.Shift = shift;
                        cellInfo = existingCell;
                        
                        // Re-apply visual state from model (always re-apply to prevent blank cells after recycling)
                        if (cellInfo.Shift != null)
                        {
                            var hour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cellInfo.Shift.ShiftHourId);
                            UpdateCellVisuals(hour, cellInfo.Shift.PoleColor, cellInfo.Shift.PoleIcon, cellGrid, border, day);
                        }
                    }
                    else
                    {
                        cellInfo = new CellInfo
                        {
                            Border = border,
                            CellGrid = cellGrid,
                            Row = row,
                            Shift = shift,
                            Day = day
                        };
                        _cellsByKey[key] = cellInfo;
                    }
                    _cellRegistry[border] = cellInfo;

                    // Re-apply selection styling if this cell is selected
                    if (_selectedCells.Contains(cellInfo))
                    {
                        border.BorderBrush = SelectionBorderBrush;
                        border.BorderThickness = FocusBorderThickness;
                    }

                    // Start drag on left press
                    border.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                        {
                            ClearSelection();
                            _isDragging = true;
                            SelectCell(cellInfo);
                            ScheduleDataGrid.SelectedItem = null; // Prevent DataGrid row selection from painting over cells
                            e.Handled = true; // Prevent DataGrid from re-rendering the row
                        }
                    };

                    // Context menu on right click - applies to all selected cells
                    border.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(border).Properties.IsRightButtonPressed)
                        {
                            // If this cell is not in selection, select only it
                            if (!_selectedCells.Contains(cellInfo))
                            {
                                ClearSelection();
                                SelectCell(cellInfo);
                            }
                            e.Handled = true; // Prevent DataGrid from re-rendering the row
                        }
                    };

                    // Flyout for multi-cell edit
                    var flyout = CreateMultiCellFlyout(cellInfo);
                    border.ContextFlyout = flyout;

                    return border;
                })
            };

            ScheduleDataGrid.Columns.Add(dayColumn);
        }

        // Add gap rows at the bottom
        _gapRow = new ScheduleRow
        {
            Id = ScheduleRow.GeneralGapRowId,
            FirstName = "Brak obsady",
            LastName = "",
            HoursSummary = 0,
            Records = new List<ScheduleColumn>()
        };
        _pharmacistGapRow = new ScheduleRow
        {
            Id = ScheduleRow.PharmacistGapRowId,
            FirstName = "Brak farmaceuty/ki",
            LastName = "",
            HoursSummary = 0,
            Records = new List<ScheduleColumn>()
        };
        var allRows = new List<ScheduleRow>(scheduleRows) { _gapRow, _pharmacistGapRow };
        ScheduleDataGrid.ItemsSource = allRows;

        // Initialize gap cache
        InitGapCache(days);
    }

    private void OnGlobalPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var point = e.GetPosition(this);
        var hit = this.InputHitTest(point);
        if (hit is Visual visual)
        {
            var border = FindParentBorder(visual);
            if (border != null && _cellRegistry.TryGetValue(border, out var cellInfo))
            {
                // Only allow selection within the same row
                if (_selectedCells.Count > 0 && _selectedCells[0].Row != cellInfo.Row)
                    return;

                if (!_selectedCells.Contains(cellInfo))
                {
                    SelectCell(cellInfo);
                }
            }
        }
    }

    private void OnGlobalPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    private Border? FindParentBorder(Visual? visual)
    {
        while (visual != null)
        {
            if (visual is Border b && _cellRegistry.ContainsKey(b))
                return b;
            visual = visual.GetVisualParent() as Visual;
        }
        return null;
    }

    private void SelectCell(CellInfo cellInfo)
    {
        _selectedCells.Add(cellInfo);
        cellInfo.Border.BorderBrush = SelectionBorderBrush;
        cellInfo.Border.BorderThickness = FocusBorderThickness;
    }

    private void ClearSelection()
    {
        foreach (var cell in _selectedCells)
        {
            cell.Border.BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
            cell.Border.BorderThickness = new Thickness(1, 0, 0, 0);
        }
        _selectedCells.Clear();
    }

    private Flyout CreateMultiCellFlyout(CellInfo triggerCell)
    {
        var flyout = new Flyout();

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

        // Set initial selection from trigger cell
        hoursListBox.SelectedItem = _contextMenu.Hours.FirstOrDefault(h => h?.Id == triggerCell.Shift?.ShiftHourId);
        colorsListBox.SelectedItem = _contextMenu.Colors.FirstOrDefault(c => c == triggerCell.Shift?.PoleColor);
        iconsListBox.SelectedItem = _contextMenu.Icons.FirstOrDefault(i => i == triggerCell.Shift?.PoleIcon);

        // Apply hours change to all selected cells
        hoursListBox.SelectionChanged += (s, e) =>
        {
            var selectedHour = hoursListBox.SelectedItem as HoursRecord;
            var employeeTable = new EmployeeTable();
            employeeTable.StartConnectionWithDatabase();

            foreach (var cell in _selectedCells.ToList())
            {
                var previousHour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cell.Shift?.ShiftHourId);
                cell.Row.HoursSummary -= CalculateShiftHours(previousHour);
                cell.Row.HoursSummary += CalculateShiftHours(selectedHour);

                // Update vacation days: decrement if old was vacation, increment if new is vacation
                bool wasVacation = previousHour?.IsVacation == true;
                bool isVacation = selectedHour?.IsVacation == true;
                if (wasVacation && !isVacation)
                {
                    employeeTable.UpdateUsedVacationDays(cell.Row.Id, -1);
                    cell.Row.UsedVacationDays = (cell.Row.UsedVacationDays ?? 0) - 1;
                }
                else if (!wasVacation && isVacation)
                {
                    employeeTable.UpdateUsedVacationDays(cell.Row.Id, 1);
                    cell.Row.UsedVacationDays = (cell.Row.UsedVacationDays ?? 0) + 1;
                }

                ApplyHourToCell(cell, selectedHour);
            }
        };

        // Apply color change to all selected cells
        colorsListBox.SelectionChanged += (s, e) =>
        {
            var selectedColor = colorsListBox.SelectedItem as string;
            foreach (var cell in _selectedCells.ToList())
            {
                ApplyColorToCell(cell, selectedColor);
            }
        };

        // Apply icon change to all selected cells
        iconsListBox.SelectionChanged += (s, e) =>
        {
            var selectedIcon = iconsListBox.SelectedItem as string;
            foreach (var cell in _selectedCells.ToList())
            {
                ApplyIconToCell(cell, selectedIcon);
            }
        };

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

        // Save all selected cells on flyout close
        flyout.Closed += (sender, args) =>
        {
            var table = new ShiftTable();
            table.StartConnectionWithDatabase();

            var editedDays = new HashSet<DateOnly>();
            foreach (var cell in _selectedCells.ToList())
            {
                SaveCellToDatabase(table, cell);
                editedDays.Add(cell.Day);
            }

            // Refresh gap indicators for edited days
            RefreshGapsForDays(editedDays);
        };

        return flyout;
    }

    private void ApplyHourToCell(CellInfo cell, HoursRecord? selectedHour)
    {
        EnsureShiftExists(cell);
        cell.Shift!.ShiftHourId = selectedHour?.Id;
        cell.Shift.Symbol = selectedHour?.Symbol ?? "";
        cell.Shift.StartTime = selectedHour?.StartTime;
        cell.Shift.EndTime = selectedHour?.EndTime;
        UpdateCellVisuals(selectedHour, cell.Shift.PoleColor, cell.Shift.PoleIcon, cell.CellGrid, cell.Border, cell.Day);
    }

    private void ApplyColorToCell(CellInfo cell, string? selectedColor)
    {
        EnsureShiftExists(cell);
        cell.Shift!.PoleColor = selectedColor;
        var hour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cell.Shift.ShiftHourId);
        UpdateCellVisuals(hour, selectedColor, cell.Shift.PoleIcon, cell.CellGrid, cell.Border, cell.Day);
    }

    private void ApplyIconToCell(CellInfo cell, string? selectedIcon)
    {
        EnsureShiftExists(cell);
        cell.Shift!.PoleIcon = selectedIcon;
        var hour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cell.Shift.ShiftHourId);
        UpdateCellVisuals(hour, cell.Shift.PoleColor, selectedIcon, cell.CellGrid, cell.Border, cell.Day);
    }

    private void EnsureShiftExists(CellInfo cell)
    {
        if (cell.Shift == null)
        {
            cell.Shift = new ScheduleColumn { ShiftDate = cell.Day };
            cell.Row.Records.Add(cell.Shift);
        }
    }

    private void SaveCellToDatabase(ShiftTable table, CellInfo cell)
    {
        if (cell.Shift == null) return;

        if (cell.Shift.Id > 0)
        {
            var record = new ShiftRecord
            {
                Id = cell.Shift.Id,
                EmployeeId = cell.Row.Id,
                ShiftDate = cell.Day,
                ShiftHourId = cell.Shift.ShiftHourId,
                PoleColor = cell.Shift.PoleColor,
                PoleIcon = cell.Shift.PoleIcon
            };
            table.UpdateShiftRecord(record);
        }
        else
        {
            var record = new ShiftRecord
            {
                EmployeeId = cell.Row.Id,
                ShiftDate = cell.Day,
                ShiftHourId = cell.Shift.ShiftHourId,
                PoleColor = cell.Shift.PoleColor,
                PoleIcon = cell.Shift.PoleIcon
            };
            table.AddShift(record);
        }
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

    // ===== Gap Indicator Methods =====

    private double CalculateDayColumnWidth(int dayCount)
    {
        // Use screen width or fallback to 1920; subtract fixed columns (130+70) and some padding
        var screenWidth = Screens.Primary?.Bounds.Width ?? 1920;
        var available = screenWidth - 200 - 40; // 200 for fixed cols, 40 for scrollbar/padding
        return Math.Max(35, available / dayCount);
    }

    private void OnDataGridLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is ScheduleRow row)
        {
            if (row.Id == ScheduleRow.GeneralGapRowId)
            {
                e.Row.Background = GapRowLabelBackground;
                e.Row.BorderBrush = GapRowSeparator;
                e.Row.BorderThickness = new Thickness(0, 3, 0, 0);
                _gapDataGridRow = e.Row;
                RecalculateGapRowHeight(_gapDataGridRow, _gapCellTextBlocks);
            }
            else if (row.Id == ScheduleRow.PharmacistGapRowId)
            {
                e.Row.Background = GapRowLabelBackground;
                _pharmacistGapDataGridRow = e.Row;
                RecalculateGapRowHeight(_pharmacistGapDataGridRow, _pharmacistGapCellTextBlocks);
            }
            else
            {
                // Normal rows use DataGrid.RowHeight (95) — no override needed.
                // Reset in case this row was previously recycled from a gap row.
                e.Row.Height = double.NaN;
                e.Row.BorderThickness = new Thickness(0);
                e.Row.Background = Brushes.White;
            }
        }
    }

    private void RecalculateGapRowHeight(DataGridRow? dgRow, Dictionary<DateOnly, TextBlock> cellTextBlocks)
    {
        if (dgRow == null) return;

        double maxHeight = 30; // minimum
        foreach (var tb in cellTextBlocks.Values)
        {
            var lineCount = 1 + tb.Text?.Count(c => c == '\n') ?? 0;
            var estimated = lineCount * tb.LineHeight + 10;
            if (estimated > maxHeight) maxHeight = estimated;
        }

        dgRow.Height = maxHeight;
        dgRow.MinHeight = 0;
    }

    private TextBlock CreateHeaderTextBlock(DateOnly day)
    {
        bool isClosed = _closedDays.Contains(day.DayOfWeek);
        bool isHoliday = _holidayDates.Contains(day);
        
        IBrush foreground;
        if (isHoliday) foreground = HolidayForeground;
        else if (isClosed) foreground = ClosedDayForeground;
        else foreground = Brushes.Black;
        
        var tb = new TextBlock
        {
            Text = day.ToString("dd.MM\nddd"),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = foreground,
            FontStyle = (isClosed || isHoliday) ? FontStyle.Italic : FontStyle.Normal,
        };
        _headerTextBlocks[day] = tb;
        return tb;
    }

    private Border CreateGapCell(DateOnly day)
    {
        var hasGap = _gapCache.TryGetValue(day, out var text);
        
        var textBlock = new TextBlock
        {
            Text = hasGap ? text! : "✓",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 10,
            LineHeight = 16,
            Foreground = hasGap ? GapHeaderForeground : CheckMarkForeground
        };
        _gapCellTextBlocks[day] = textBlock;

        return new Border
        {
            Background = hasGap ? GapRowBackground : GapRowLabelBackground,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderThickness = new Thickness(1, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
            Child = textBlock
        };
    }

    private Border CreatePharmacistGapCell(DateOnly day)
    {
        var hasGap = _pharmacistGapCache.TryGetValue(day, out var text);
        
        var textBlock = new TextBlock
        {
            Text = hasGap ? text! : "✓",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 10,
            LineHeight = 16,
            Foreground = hasGap ? PharmacistGapForeground : CheckMarkForeground
        };
        _pharmacistGapCellTextBlocks[day] = textBlock;

        return new Border
        {
            Background = hasGap ? PharmacistGapRowBackground : GapRowLabelBackground,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            BorderThickness = new Thickness(1, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC")),
            Child = textBlock
        };
    }

    private void InitGapCache(List<DateOnly> days)
    {
        var firstDay = days.FirstOrDefault();
        if (firstDay == default) return;

        var allGaps = _analiser.CheckEmptyHoursInSchedule(_scheduleRows, firstDay.Month, firstDay.Year);
        _gapCache = GapFormatter.FormatGaps(allGaps);

        var pharmacistGaps = _analiser.CheckPharmacistGapsInSchedule(_scheduleRows, firstDay.Month, firstDay.Year);
        _pharmacistGapCache = GapFormatter.FormatGaps(pharmacistGaps);

        // Apply colors to headers
        foreach (var day in days)
        {
            UpdateColumnHeader(day);
        }
        
        // Update gap row cells
        foreach (var day in days)
        {
            UpdateGapRowCell(day);
            UpdatePharmacistGapRowCell(day);
        }
        
    }

    private void RefreshGapForDay(DateOnly editedDay)
    {
        var dayGaps = _analiser.CheckOneDay(_scheduleRows, editedDay);

        if (dayGaps.Count == 0)
            _gapCache.Remove(editedDay);
        else
            _gapCache[editedDay] = GapFormatter.FormatGaps(dayGaps)[editedDay];

        var pharmacistGaps = _analiser.CheckOneDayForPharmacist(_scheduleRows, editedDay);

        if (pharmacistGaps.Count == 0)
            _pharmacistGapCache.Remove(editedDay);
        else
            _pharmacistGapCache[editedDay] = GapFormatter.FormatGaps(pharmacistGaps)[editedDay];

        UpdateColumnHeader(editedDay);
        UpdateGapRowCell(editedDay);
        UpdatePharmacistGapRowCell(editedDay);
    }

    private void RefreshGapsForDays(IEnumerable<DateOnly> days)
    {
        foreach (var day in days.Distinct())
        {
            RefreshGapForDay(day);
        }
    }

    private void UpdateColumnHeader(DateOnly day)
    {
        if (!_headerTextBlocks.TryGetValue(day, out var tb))
        {
            return;
        }

        var hasGap = _gapCache.ContainsKey(day);
        tb.Foreground = hasGap ? GapHeaderForeground : Brushes.Black;
        tb.FontWeight = hasGap ? FontWeight.Bold : FontWeight.Normal;
    }

    private void UpdateGapRowCell(DateOnly day)
    {
        if (!_gapCellTextBlocks.TryGetValue(day, out var tb))
        {
            return;
        }

        var hasGap = _gapCache.TryGetValue(day, out var text);
        tb.Text = hasGap ? text : "✓";
        tb.Foreground = hasGap ? GapHeaderForeground : CheckMarkForeground;

        if (tb.Parent is Border border)
        {
            border.Background = hasGap ? GapRowBackground : GapRowLabelBackground;
        }

        RecalculateGapRowHeight(_gapDataGridRow, _gapCellTextBlocks);
    }

    private void UpdatePharmacistGapRowCell(DateOnly day)
    {
        if (!_pharmacistGapCellTextBlocks.TryGetValue(day, out var tb))
        {
            return;
        }

        var hasGap = _pharmacistGapCache.TryGetValue(day, out var text);
        tb.Text = hasGap ? text : "✓";
        tb.Foreground = hasGap ? PharmacistGapForeground : CheckMarkForeground;

        if (tb.Parent is Border border)
        {
            border.Background = hasGap ? PharmacistGapRowBackground : GapRowLabelBackground;
        }

        RecalculateGapRowHeight(_pharmacistGapDataGridRow, _pharmacistGapCellTextBlocks);
    }

    private HashSet<DayOfWeek> LoadClosedDays()
    {
        var settingsTable = new SettingsTable();
        settingsTable.StartConnectionWithDatabase();
        var settings = settingsTable.GetSettings();
        if (settings == null) return new HashSet<DayOfWeek>();

        var closed = new HashSet<DayOfWeek>();
        if (!settings.MondayOpen) closed.Add(DayOfWeek.Monday);
        if (!settings.TuesdayOpen) closed.Add(DayOfWeek.Tuesday);
        if (!settings.WednesdayOpen) closed.Add(DayOfWeek.Wednesday);
        if (!settings.ThursdayOpen) closed.Add(DayOfWeek.Thursday);
        if (!settings.FridayOpen) closed.Add(DayOfWeek.Friday);
        if (!settings.SaturdayOpen) closed.Add(DayOfWeek.Saturday);
        if (!settings.SundayOpen) closed.Add(DayOfWeek.Sunday);
        return closed;
    }

    private HashSet<DateOnly> LoadHolidayDates(List<ScheduleRow> scheduleRows)
    {
        if (scheduleRows == null || !scheduleRows.Any() || 
            scheduleRows[0].Records == null || !scheduleRows[0].Records.Any())
            return new HashSet<DateOnly>();

        var firstDate = scheduleRows[0].Records[0].ShiftDate;
        int year = firstDate.Year;
        int month = firstDate.Month;

        var holidaysTable = new HolidaysTable();
        holidaysTable.StartConnectionWithDatabase();
        PolishHolidays.SeedBuiltIn(holidaysTable);
        
        return PolishHolidays.GetActiveHolidayDatesForMonth(holidaysTable, year, month);
    }
}

public class LegendItem
{
    public string Symbol { get; set; } = "";
    public string Description { get; set; } = "";
}
