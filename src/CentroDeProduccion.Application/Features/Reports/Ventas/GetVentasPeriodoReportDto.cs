using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the sales-by-period report: one row per grouped period with the number of delivery
/// notes, the total delivered quantity and the total subtotal of each period.
/// </summary>
public sealed record GetVentasPeriodoReportDto(
    IReadOnlyList<VentasPeriodoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("periodo", "Período"),
            new("remitos", "Remitos"),
            new("cantidadTotal", "Cantidad total"),
            new("totalSubtotal", "Total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.PeriodoLabel, i.RemitosCount, i.CantidadTotal, i.TotalSubtotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "ventas-periodo",
            Metadata.ReportTitle ?? "Ventas por período",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One period row of the sales-by-period report.</summary>
public sealed record VentasPeriodoReportItem(
    string PeriodoLabel,
    int RemitosCount,
    decimal CantidadTotal,
    decimal TotalSubtotal);
