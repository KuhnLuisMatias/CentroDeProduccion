using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the sales-by-bar report: one row per bar with the number of delivery notes, the total
/// number of lines and the total subtotal of delivered goods.
/// </summary>
public sealed record GetVentasPorBarReportDto(
    IReadOnlyList<VentasPorBarReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("bar", "Bar"),
            new("remitos", "Remitos"),
            new("lineas", "Líneas"),
            new("totalSubtotal", "Total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.BarNombre, i.RemitosCount, i.LineasCount, i.TotalSubtotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "ventas-por-bar",
            Metadata.ReportTitle ?? "Ventas por bar",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One bar row of the sales-by-bar report.</summary>
public sealed record VentasPorBarReportItem(
    Guid BarId,
    string BarNombre,
    int RemitosCount,
    int LineasCount,
    decimal TotalSubtotal);
