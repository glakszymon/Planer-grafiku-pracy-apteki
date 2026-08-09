using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using GrafikPlanerCore.Models;

namespace GrafikPlanerUI.Views;

public partial class ScheduleTableWindow : Window
{
    public ScheduleTableWindow()
    {
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
                Header = day.ToString("dd.MM\nddd"),
                Width = new DataGridLength(60),
                CellTemplate = new FuncDataTemplate<ScheduleRow>((row, namescope) =>
                {
                    var shift = row?.Records.FirstOrDefault(r => r.ShiftDate == day);

                    return new TextBlock
                    {
                        Text = shift?.ShiftHourId?.ToString() ?? "-", 
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
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
}