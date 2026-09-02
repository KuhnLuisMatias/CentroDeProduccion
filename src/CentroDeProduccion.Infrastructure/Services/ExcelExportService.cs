using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Common;
using ClosedXML.Excel;

namespace CentroDeProduccion.Infrastructure.Services;

/// <summary>
/// Exports a <see cref="ReportTable"/> to a .xlsx byte stream using ClosedXML. Each export is a
/// single worksheet named after <see cref="ReportTable.ReportType"/> (truncated to Excel's 31-char
/// limit) with a bold, filled header row, autofitted columns and the first row frozen.
/// </summary>
public class ExcelExportService : IExportService
{
    private const int MaxSheetNameLength = 31;

    public Task<byte[]> ExportToExcelAsync(ReportTable report, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();
        var sheetName = SanitizeSheetName(report.ReportType);
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Header row
        for (var col = 0; col < report.Columns.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = report.Columns[col].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data rows (in column order)
        for (var row = 0; row < report.Rows.Count; row++)
        {
            var source = report.Rows[row].Cells;
            for (var col = 0; col < report.Columns.Count; col++)
            {
                var cell = worksheet.Cell(row + 2, col + 1);
                if (col < source.Count && source[col] is not null)
                {
                    cell.Value = XLCellValue.FromObject(source[col]);
                }
            }
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> ExportToPdfAsync(ReportTable report, CancellationToken ct = default)
    {
        throw new NotSupportedException("ExcelExportService only exports Excel documents.");
    }

    private static string SanitizeSheetName(string? name)
    {
        var sanitized = name ?? "Report";
        foreach (var invalid in new[] { '\\', '/', '*', '?', ':', '[', ']' })
        {
            sanitized = sanitized.Replace(invalid, '_');
        }

        if (sanitized.Length > MaxSheetNameLength)
        {
            sanitized = sanitized[..MaxSheetNameLength];
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "Report" : sanitized;
    }
}
