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

    public ScheduleExportService(List<HoursRecord> hours)
    {
        _hours = hours;
    }

    public void ExportToExcel(string filePath, List<ScheduleRow> rows, bool includeColors, bool includeLegend)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Grafik");

        var days = rows.SelectMany(r => r.Records).Select(c => c.ShiftDate).Distinct().OrderBy(d => d).ToList();

        // Header row
        ws.Cell(1, 1).Value = "Pracownik";
        ws.Cell(1, 1).Style.Font.Bold = true;
        for (int i = 0; i < days.Count; i++)
        {
            var cell = ws.Cell(1, i + 2);
            cell.Value = days[i].ToString("dd.MM");
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            ws.Cell(r + 2, 1).Value = $"{row.FirstName} {row.LastName}";

            for (int d = 0; d < days.Count; d++)
            {
                var shift = row.Records?.FirstOrDefault(rec => rec.ShiftDate == days[d]);
                var cell = ws.Cell(r + 2, d + 2);

                cell.Value = shift?.Symbol ?? "";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

                // Weekend shading
                if (includeColors && string.IsNullOrWhiteSpace(shift?.PoleColor) &&
                    (days[d].DayOfWeek == DayOfWeek.Saturday || days[d].DayOfWeek == DayOfWeek.Sunday))
                {
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F7FAFC");
                }
            }
        }

        // Legend sheet
        if (includeLegend && _hours.Count > 0)
        {
            var legendWs = workbook.Worksheets.Add("Legenda");
            legendWs.Cell(1, 1).Value = "Symbol";
            legendWs.Cell(1, 2).Value = "Godziny";
            legendWs.Cell(1, 1).Style.Font.Bold = true;
            legendWs.Cell(1, 2).Style.Font.Bold = true;

            for (int i = 0; i < _hours.Count; i++)
            {
                legendWs.Cell(i + 2, 1).Value = _hours[i].Symbol;
                legendWs.Cell(i + 2, 2).Value = $"{_hours[i].StartTime:HH:mm} – {_hours[i].EndTime:HH:mm}";
            }
            legendWs.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }

    public void ExportToPdf(string filePath, List<ScheduleRow> rows, bool includeColors, bool includeLegend)
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
                        columns.RelativeColumn(3); // Employee name
                        foreach (var _ in days)
                            columns.RelativeColumn(1);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(3)
                            .Text("Pracownik").Bold().FontSize(7);

                        foreach (var day in days)
                        {
                            header.Cell().Border(0.5f).Background(Colors.Grey.Lighten3).Padding(2)
                                .AlignCenter().Text($"{day:dd.MM}\n{day:ddd}").FontSize(6).Bold();
                        }
                    });

                    // Data
                    foreach (var row in rows)
                    {
                        table.Cell().Border(0.5f).PaddingHorizontal(4).PaddingVertical(10).AlignMiddle()
                            .Text($"{row.FirstName} {row.LastName}").FontSize(7);

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
                            else if (includeColors && (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday))
                            {
                                cellDescriptor = cellDescriptor.Background(Colors.Grey.Lighten4);
                            }

                            var symbol = shift?.Symbol ?? "";

                            cellDescriptor.PaddingHorizontal(4).PaddingVertical(10).AlignCenter().AlignMiddle()
                                .Text(symbol).FontSize(7);
                        }
                    }
                });

                // Legend
                if (includeLegend && _hours.Count > 0)
                {
                    page.Footer().PaddingTop(10).Row(footerRow =>
                    {
                        footerRow.AutoItem().Text("Legenda: ").Bold().FontSize(7);
                        var legendText = string.Join("  |  ", _hours.Select(h => $"{h.Symbol}: {h.StartTime:HH:mm}–{h.EndTime:HH:mm}"));
                        footerRow.RelativeItem().Text(legendText).FontSize(7);
                    });
                }
            });
        }).GeneratePdf(filePath);
    }
}
