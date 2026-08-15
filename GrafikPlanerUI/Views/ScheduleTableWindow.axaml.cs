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
using Avalonia.Threading;
using Avalonia.VisualTree;
using GrafikPlanerCore;
using GrafikPlanerCore.Models;
using GrafikPlanerData.DbScripts;
using GrafikPlanerData.Models;
using GrafikPlanerUI.ViewModels;

namespace GrafikPlanerUI.Views;

public partial class ScheduleTableWindow : Window
{
    private readonly ContextMenuOptions _contextMenu;
    private static readonly IBrush WeekendBackground = new SolidColorBrush(Color.Parse("#F0F0F0"));
    private static readonly Thickness FocusBorderThickness = new Thickness(3);
    private static readonly IBrush SelectionBorderBrush = new SolidColorBrush(Color.Parse("#3182CE"));

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

        // Global pointer events for drag selection
        PointerMoved += OnGlobalPointerMoved;
        PointerReleased += OnGlobalPointerReleased;
    }

    private void BackButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        mainWindow.Show();
        this.Close();
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

        var days = scheduleRows
            .SelectMany(r => r.Records)
            .Select(c => c.ShiftDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var lastColumn = ScheduleDataGrid.Columns.LastOrDefault();
        if (lastColumn != null)
        {
            ScheduleDataGrid.Columns.Remove(lastColumn);
        }

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
                    if (row == null) return new Border();

                    var shift = row.Records?.FirstOrDefault(r => r.ShiftDate == day);
                    bool isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;

                    System.Diagnostics.Debug.WriteLine($"[TEMPLATE] Creating cell for Row={row.FirstName} {row.LastName}, Day={day}, Shift={shift?.Symbol ?? "null"}, ShiftId={shift?.Id}");

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
                        System.Diagnostics.Debug.WriteLine($"[TEMPLATE-REUSE] Re-creating for Row={row.FirstName}, Day={day}. Old border hash={existingCell.Border.GetHashCode()}, New={border.GetHashCode()}");
                        // Template was re-created, update references
                        _cellRegistry.Remove(existingCell.Border);
                        existingCell.Border = border;
                        existingCell.CellGrid = cellGrid;
                        // Update shift reference - it may have been created by EnsureShiftExists
                        if (existingCell.Shift == null && shift != null)
                            existingCell.Shift = shift;
                        cellInfo = existingCell;
                        
                        // Re-apply visual state from model (shift may have been modified)
                        if (cellInfo.Shift != null && (cellInfo.Shift.ShiftHourId != null || 
                            !string.IsNullOrWhiteSpace(cellInfo.Shift.PoleColor) || 
                            !string.IsNullOrWhiteSpace(cellInfo.Shift.PoleIcon)))
                        {
                            var hour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cellInfo.Shift.ShiftHourId);
                            System.Diagnostics.Debug.WriteLine($"[TEMPLATE-REUSE] Re-applying visuals: Symbol={hour?.Symbol}, Color={cellInfo.Shift.PoleColor}, Icon={cellInfo.Shift.PoleIcon}");
                            UpdateCellVisuals(hour, cellInfo.Shift.PoleColor, cellInfo.Shift.PoleIcon, cellGrid, border, day);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[TEMPLATE-REUSE] No visuals to re-apply. Shift null={cellInfo.Shift == null}, HourId={cellInfo.Shift?.ShiftHourId}, Color={cellInfo.Shift?.PoleColor}, Icon={cellInfo.Shift?.PoleIcon}");
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
                        System.Diagnostics.Debug.WriteLine($"[TEMPLATE-NEW] Registered new cell for Row={row.FirstName}, Day={day}");
                    }
                    _cellRegistry[border] = cellInfo;

                    // Re-apply selection styling if this cell is selected
                    if (_selectedCells.Contains(cellInfo))
                    {
                        border.BorderBrush = SelectionBorderBrush;
                        border.BorderThickness = FocusBorderThickness;
                        System.Diagnostics.Debug.WriteLine($"[TEMPLATE] Re-applied selection styling for Row={row.FirstName}, Day={day}");
                    }

                    // Start drag on left press
                    border.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CLICK-LEFT] Row={cellInfo.Row.FirstName}, Day={cellInfo.Day}, SelectedCells before clear={_selectedCells.Count}");
                            ClearSelection();
                            _isDragging = true;
                            SelectCell(cellInfo);
                            System.Diagnostics.Debug.WriteLine($"[CLICK-LEFT] After select. SelectedCells={_selectedCells.Count}");
                            e.Handled = true; // Prevent DataGrid from re-rendering the row
                        }
                    };

                    // Context menu on right click - applies to all selected cells
                    border.PointerPressed += (s, e) =>
                    {
                        if (e.GetCurrentPoint(border).Properties.IsRightButtonPressed)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CLICK-RIGHT] Row={cellInfo.Row.FirstName}, Day={cellInfo.Day}, SelectedCells={_selectedCells.Count}");
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

        if (lastColumn != null)
        {
            ScheduleDataGrid.Columns.Add(lastColumn);
        }

        ScheduleDataGrid.ItemsSource = scheduleRows;
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
                    System.Diagnostics.Debug.WriteLine($"[DRAG] Adding cell Row={cellInfo.Row.FirstName}, Day={cellInfo.Day}. Total selected={_selectedCells.Count + 1}");
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
        System.Diagnostics.Debug.WriteLine($"[CLEAR] Clearing {_selectedCells.Count} cells");
        foreach (var cell in _selectedCells)
        {
            System.Diagnostics.Debug.WriteLine($"[CLEAR] Resetting border for Row={cell.Row.FirstName}, Day={cell.Day}, Border hash={cell.Border.GetHashCode()}, HasParent={cell.Border.GetVisualParent() != null}");
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
            foreach (var cell in _selectedCells.ToList())
            {
                var previousHour = _contextMenu.Hours.FirstOrDefault(h => h?.Id == cell.Shift?.ShiftHourId);
                cell.Row.HoursSummary -= CalculateShiftHours(previousHour);
                cell.Row.HoursSummary += CalculateShiftHours(selectedHour);

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

            foreach (var cell in _selectedCells.ToList())
            {
                SaveCellToDatabase(table, cell);
            }
        };

        return flyout;
    }

    private void ApplyHourToCell(CellInfo cell, HoursRecord? selectedHour)
    {
        EnsureShiftExists(cell);
        cell.Shift!.ShiftHourId = selectedHour?.Id;
        cell.Shift.Symbol = selectedHour?.Symbol ?? "";
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
}
