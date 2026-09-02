using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Common;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CentroDeProduccion.Infrastructure.Services;

/// <summary>
/// Exports a <see cref="ReportTable"/> to a PDF byte stream using QuestPDF. Produces an A4
/// document with a title/header, a data table, and page numbers in the footer. The page is
/// rendered in landscape when the report has more than five columns.
/// </summary>
public class PdfExportService : IExportService
{
    private const int LandscapeColumnThreshold = 5;

    public Task<byte[]> ExportToPdfAsync(ReportTable report, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        QuestPDF.Settings.License = LicenseType.Community;

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(report.Columns.Count > LandscapeColumnThreshold
                    ? PageSizes.A4.Landscape()
                    : PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().PaddingBottom(12).Column(header =>
                {
                    header.Spacing(4);
                    header.Item().Text(report.ReportTitle).FontSize(16).SemiBold();
                    header.Item().Text($"Generado: {report.Metadata.GeneratedAt:g}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in report.Columns)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    table.Header(header =>
                    {
                        foreach (var column in report.Columns)
                        {
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(6).Text(column.Header).SemiBold();
                        }
                    });

                    if (report.Rows.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)report.Columns.Count)
                            .Padding(12).AlignCenter().Text("No hay datos para el rango seleccionado.");
                    }
                    else
                    {
                        foreach (var row in report.Rows)
                        {
                            for (var col = 0; col < report.Columns.Count; col++)
                            {
                                var value = col < row.Cells.Count ? row.Cells[col] : null;
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(6).Text(value?.ToString() ?? string.Empty);
                            }
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    public Task<byte[]> ExportToExcelAsync(ReportTable report, CancellationToken ct = default)
    {
        throw new NotSupportedException("PdfExportService only exports PDF documents.");
    }
}
