using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Result of the profitability report per finished product: revenue from delivered remitos, cost
/// attributed from the producing recipe, and the resulting profit and margin.
/// </summary>
public sealed record GetRentabilidadProductoReportDto(
    IReadOnlyList<RentabilidadProductoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("producto", "Producto"),
            new("ingresos", "Ingresos", "C2"),
            new("costos", "Costos", "C2"),
            new("rentabilidad", "Rentabilidad", "C2"),
            new("margenPorcentaje", "Margen %", "P2"),
            new("observacion", "Observación")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.ProductoTerminadoNombre, i.Ingresos, i.Costos, i.Rentabilidad, i.MargenPorcentaje / 100m, i.Observacion
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "rentabilidad-producto",
            Metadata.ReportTitle ?? "Rentabilidad por producto",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One finished product row of the profitability report.</summary>
public sealed record RentabilidadProductoReportItem(
    Guid ProductoTerminadoId,
    string ProductoTerminadoNombre,
    decimal Ingresos,
    decimal Costos,
    decimal Rentabilidad,
    decimal MargenPorcentaje,
    string? Observacion);
