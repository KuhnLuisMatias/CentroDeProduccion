using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Result of the production-by-period report: one row per grouped period with the number of
/// production runs, the total produced quantity and the total cost of each period.
/// </summary>
public sealed record GetProduccionPeriodoReportDto(
    IReadOnlyList<ProduccionPeriodoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("periodo", "Período"),
            new("cantidadProducciones", "Cant. producciones"),
            new("cantidadProducida", "Cantidad producida"),
            new("costoTotal", "Costo total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.PeriodoLabel, i.CantidadProducciones, i.CantidadProducida, i.CostoTotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "produccion-periodo",
            Metadata.ReportTitle ?? "Producción por período",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One period row of the production-by-period report.</summary>
public sealed record ProduccionPeriodoReportItem(
    string PeriodoLabel,
    int CantidadProducciones,
    decimal CantidadProducida,
    decimal CostoTotal);
