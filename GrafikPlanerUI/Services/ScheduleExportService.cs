using ClosedXML.Excel;
using GrafikPlanerCore.Models;
using GrafikPlanerData.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GrafikPlanerUI.Services;

public class ScheduleExportService
{
    private readonly List<HoursRecord> _hours;
    private readonly IReadOnlyDictionary<DateOnly, string> _holidayNames;

    public ScheduleExportService(List<HoursRecord> hours, IReadOnlyDictionary<DateOnly, string>? holidayNames = null)
    {
        _hours = hours;
        _holidayNames = holidayNames ?? new Dictionary<DateOnly, string>();
    }

    private static string GetLegendDescription(HoursRecord h)
    {
        if (h.IsSickLeave)
            return $"L4 {(int)(h.EndTime - h.StartTime).TotalHours} godzinne";
        return h.IsVacation
            ? $"urlop {(int)(h.EndTime - h.StartTime).TotalHours} godzinny"
            : $"{h.StartTime:HH:mm} – {h.EndTime:HH:mm}";
    }

    public void ExportToExcel(string filePath, List<ScheduleRow> rows, bool includeColors, bool includeSpecialization, bool includeHoursSummary, bool includeLegend)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Grafik");

        var days = rows.SelectMany(r => r.Records).Select(c => c.ShiftDate).Distinct().OrderBy(d => d).ToList();

        int firstDayCol = 1;
        var headerCell = ws.Cell(1, 1);
        headerCell.Value = "Pracownik";
        headerCell.Style.Font.Bold = true;

        if (includeHoursSummary)
        {
            ws.Cell(1, ++firstDayCol).Value = "Suma godzin";
            ws.Cell(1, firstDayCol).Style.Font.Bold = true;
        }
        firstDayCol++;

        for (int i = 0; i < days.Count; i++)
        {
            var cell = ws.Cell(1, firstDayCol + i);
            cell.Value = days[i].ToString("dd.MM");
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (_holidayNames.ContainsKey(days[i]))
            {
                cell.Style.Font.Italic = true;
                cell.Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
            }
        }

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];

            var nameCell = ws.Cell(r + 2, 1);
            nameCell.Value = includeSpecialization && !string.IsNullOrWhiteSpace(row.Specialisation)
                ? $"{row.FirstName} {row.LastName} ({row.Specialisation})"
                : $"{row.FirstName} {row.LastName}";

            int c = 2;
            if (includeHoursSummary)
            {
                var hoursCell = ws.Cell(r + 2, c++);
                hoursCell.Value = row.HoursSummary;
                hoursCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int d = 0; d < days.Count; d++)
            {
                var shift = row.Records?.FirstOrDefault(rec => rec.ShiftDate == days[d]);
                var cell = ws.Cell(r + 2, firstDayCol + d);

                var holidayName = _holidayNames.ContainsKey(days[d]) ? _holidayNames[days[d]] : null;
                cell.Value = shift?.Symbol ?? (holidayName != null ? "-" : "");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (holidayName != null && string.IsNullOrEmpty(shift?.Symbol))
                {
                    cell.Style.Font.Italic = true;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
                }

                if (includeColors && !string.IsNullOrWhiteSpace(shift?.PoleColor))
                {
                    try
                    {
                        var hex = shift.PoleColor.TrimStart('#');
                        var color = XLColor.FromHtml($"#{hex}");
                        cell.Style.Fill.BackgroundColor = color;
                    }
                    catch { }
                }

                // Holiday shading
                if (includeColors && holidayName != null)
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FCA5A5");
                }

                // Weekend shading
                if (includeColors && holidayName == null && string.IsNullOrWhiteSpace(shift?.PoleColor) &&
                    (days[d].DayOfWeek == DayOfWeek.Saturday || days[d].DayOfWeek == DayOfWeek.Sunday))
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                }
            }
        }

        // Legend in the same sheet, below the data
        if (includeLegend && _hours.Count > 0)
        {
            int legendRow = rows.Count + 4;

            var legendTitle = ws.Cell(legendRow, 1);
            legendTitle.Value = "Legenda";
            legendTitle.Style.Font.Bold = true;

            ws.Cell(legendRow + 1, 1).Value = "Symbol";
            ws.Cell(legendRow + 1, 2).Value = "Godziny";
            ws.Cell(legendRow + 1, 1).Style.Font.Bold = true;
            ws.Cell(legendRow + 1, 2).Style.Font.Bold = true;

            for (int i = 0; i < _hours.Count; i++)
            {
                ws.Cell(legendRow + 2 + i, 1).Value = _hours[i].Symbol;
                ws.Cell(legendRow + 2 + i, 2).Value = GetLegendDescription(_hours[i]);
            }
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportToPdf(string filePath, List<ScheduleRow> rows, bool includeColors, bool includeSpecialization, bool includeHoursSummary, bool includeLegend)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var days = rows.SelectMany(r => r.Records).Select(c => c.ShiftDate).Distinct().OrderBy(d => d).ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Text("Grafik Pracy").FontSize(16).Bold().AlignCenter();

                page.Content().PaddingVertical(10).Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Employee name (+ specialization)
                        if (includeHoursSummary)
                            columns.RelativeColumn(1.5f);
                        foreach (var _ in days)
                            columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Pracownik").Bold().FontSize(7);

                        if (includeHoursSummary)
                        {
                            header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3)
                                .AlignCenter().Text("Suma").Bold().FontSize(7);
                        }

                        foreach (var day in days)
                        {
                            var dayCell = header.Cell().Border(0.5f).Background(
                                _holidayNames.ContainsKey(day) ? Colors.Red.Lighten3 : Colors.Grey.Lighten3);
                            dayCell.Padding(2)
                                .AlignCenter().Text($"{day:dd.MM}\n{day:ddd}").FontSize(6).Bold();
                        }
                    });

                    // Data
                    foreach (var row in rows)
                    {
                        var employeeLabel = includeSpecialization && !string.IsNullOrWhiteSpace(row.Specialisation)
                            ? $"{row.FirstName} {row.LastName}\n{row.Specialisation}"
                            : $"{row.FirstName} {row.LastName}";

                        table.Cell().Border(0.5f).PaddingHorizontal(4).PaddingVertical(3).AlignMiddle()
                            .Text(employeeLabel).FontSize(7);

                        if (includeHoursSummary)
                        {
                            table.Cell().Border(0.5f).PaddingHorizontal(4).PaddingVertical(3).AlignMiddle().AlignCenter()
                                .Text(row.HoursSummary.ToString()).FontSize(7);
                        }

                        foreach (var day in days)
                        {
                            var shift = row.Records?.FirstOrDefault(rec => rec.ShiftDate == day);
                            var cellDescriptor = table.Cell().Border(0.5f);

                            // Background color
                            if (includeColors && !string.IsNullOrWhiteSpace(shift?.PoleColor))
                            {
                                try
                                {
                                    var hex = shift.PoleColor.TrimStart('#');
                                    if (hex.Length == 6)
                                    {
                                        var r2 = Convert.ToByte(hex.Substring(0, 2), 16);
                                        var g = Convert.ToByte(hex.Substring(2, 2), 16);
                                        var b = Convert.ToByte(hex.Substring(4, 2), 16);
                                        cellDescriptor = cellDescriptor.Background(Color.FromRGB(r2, g, b));
                                    }
                                }
                                catch { }
                            }
                            else if (includeColors && _holidayNames.ContainsKey(day))
                            {
                                cellDescriptor = cellDescriptor.Background(Colors.Red.Lighten3);
                            }
                            else if (includeColors && (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday))
                            {
                                cellDescriptor = cellDescriptor.Background(Colors.Grey.Lighten4);
                            }

                            var symbol = shift?.Symbol ?? "";
                            var holidayName = _holidayNames.TryGetValue(day, out var name) ? name : null;

                            if (string.IsNullOrEmpty(symbol) && holidayName != null)
                            {
                                cellDescriptor.PaddingHorizontal(4).PaddingVertical(3).AlignCenter().AlignMiddle()
                                    .Text("-", QuestPDF.Infrastructure.TextStyle.Default.FontSize(6).Bold().FontColor(Colors.Red.Darken4));
                            }
                            else
                            {
                                cellDescriptor.PaddingHorizontal(4).PaddingVertical(3).AlignCenter().AlignMiddle()
                                    .Text(symbol).FontSize(7);
                            }
                        }
                    }
                });

                // Legend
                if (includeLegend && _hours.Count > 0)
                {
                    page.Footer().PaddingTop(10).Row(footerRow =>
                    {
                        footerRow.AutoItem().Text("Legenda: ").Bold().FontSize(7);
                        var legendText = string.Join("  |  ", _hours.Select(h => $"{h.Symbol}: {GetLegendDescription(h)}"));
                        footerRow.RelativeItem().Text(legendText).FontSize(7);
                    });
                }
            });
        }).GeneratePdf(filePath);
    }
}
