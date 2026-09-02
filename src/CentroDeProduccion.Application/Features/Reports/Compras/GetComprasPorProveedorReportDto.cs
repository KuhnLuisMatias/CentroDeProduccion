using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Result of the purchases-by-supplier report: one row per supplier with the number of purchase
/// orders, the total amount (Σ item subtotals) and the per-status counts.
/// </summary>
public sealed record GetComprasPorProveedorReportDto(
    IReadOnlyList<ComprasPorProveedorReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("proveedor", "Proveedor"),
            new("ordenes", "Órdenes"),
            new("totalMonto", "Total", "C2"),
            new("pendientes", "Pendientes"),
            new("canceladas", "Canceladas")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.ProveedorNombre, i.OrdenesCount, i.TotalMonto, i.Pendientes, i.Canceladas
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "compras-por-proveedor",
            Metadata.ReportTitle ?? "Compras por proveedor",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One supplier row of the purchases-by-supplier report.</summary>
public sealed record ComprasPorProveedorReportItem(
    Guid ProveedorId,
    string ProveedorNombre,
    int OrdenesCount,
    decimal TotalMonto,
    int Pendientes,
    int Canceladas);
