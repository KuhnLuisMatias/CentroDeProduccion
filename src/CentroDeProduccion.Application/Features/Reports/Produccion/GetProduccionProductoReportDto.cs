using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Result of the production-by-recipe report: one row per recipe with production count, total
/// produced quantity and average production cost.
/// </summary>
public sealed record GetProduccionProductoReportDto(
    IReadOnlyList<ProduccionProductoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("receta", "Receta"),
            new("cantidadProducciones", "Cant. producciones"),
            new("cantidadProducida", "Cantidad producida"),
            new("costoPromedio", "Costo promedio", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.RecetaNombre, i.CantidadProducciones, i.CantidadProducida, i.CostoPromedio
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "produccion-producto",
            Metadata.ReportTitle ?? "Producción por producto",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One recipe row of the production-by-recipe report.</summary>
public sealed record ProduccionProductoReportItem(
    Guid RecetaId,
    string RecetaNombre,
    int CantidadProducciones,
    decimal CantidadProducida,
    decimal CostoPromedio);
