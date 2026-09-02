using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Result of the cost report per product: one row per recipe with the accumulated insumo cost,
/// total cost and the number of confirmed productions. Recipes without confirmed
/// productions in range fall back to the recipe's standard cost and carry an observation.
/// </summary>
public sealed record GetCostoProductoReportDto(
    IReadOnlyList<CostoProductoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("receta", "Receta"),
            new("costoInsumos", "Costo insumos", "C2"),
            new("costoTotal", "Costo total", "C2"),
            new("numeroProducciones", "N° producciones"),
            new("observacion", "Observación")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.RecetaNombre, i.CostoInsumos, i.CostoTotal, i.NumeroProducciones, i.Observacion
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "costo-producto",
            Metadata.ReportTitle ?? "Costo por producto",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One recipe row of the cost per product report.</summary>
public sealed record CostoProductoReportItem(
    Guid RecetaId,
    string RecetaNombre,
    decimal CostoInsumos,
    decimal CostoTotal,
    int NumeroProducciones,
    string? Observacion);
