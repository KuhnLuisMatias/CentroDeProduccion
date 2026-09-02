using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Result of the input-price-evolution report: one row per purchase movement, ordered by date,
/// with the unit price paid and the supplier it was bought from.
/// </summary>
public sealed record GetEvolucionPreciosReportDto(
    IReadOnlyList<EvolucionPreciosReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("insumo", "Insumo"),
            new("fecha", "Fecha", "dd/MM/yyyy"),
            new("precioUnitario", "Precio unitario", "C2"),
            new("proveedor", "Proveedor")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.InsumoNombre, i.Fecha, i.PrecioUnitario, i.ProveedorNombre
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "evolucion-precios",
            Metadata.ReportTitle ?? "Evolución de precios",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One purchase movement row of the price-evolution report.</summary>
public sealed record EvolucionPreciosReportItem(
    Guid InsumoId,
    string InsumoNombre,
    DateTime Fecha,
    decimal PrecioUnitario,
    string? ProveedorNombre);
