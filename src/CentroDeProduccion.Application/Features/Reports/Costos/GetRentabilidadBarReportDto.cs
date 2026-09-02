using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Result of the profitability report per bar: revenue from delivered remitos and cost attributed
/// from the products sold to the bar, with the resulting profit and margin.
/// </summary>
public sealed record GetRentabilidadBarReportDto(
    IReadOnlyList<RentabilidadBarReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("bar", "Bar"),
            new("ingresos", "Ingresos", "C2"),
            new("costos", "Costos", "C2"),
            new("rentabilidad", "Rentabilidad", "C2"),
            new("margenPorcentaje", "Margen %", "P2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.BarNombre, i.Ingresos, i.Costos, i.Rentabilidad, i.MargenPorcentaje / 100m
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "rentabilidad-bar",
            Metadata.ReportTitle ?? "Rentabilidad por bar",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One bar row of the profitability report.</summary>
public sealed record RentabilidadBarReportItem(
    Guid BarId,
    string BarNombre,
    decimal Ingresos,
    decimal Costos,
    decimal Rentabilidad,
    decimal MargenPorcentaje);
