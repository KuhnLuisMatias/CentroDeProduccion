using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Result of the suppliers summary report: one row per supplier with purchase-order totals for the
/// range and the current outstanding balance.
/// </summary>
public sealed record GetResumenProveedoresReportDto(
    IReadOnlyList<ResumenProveedoresReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("proveedor", "Proveedor"),
            new("ordenes", "Órdenes"),
            new("totalMonto", "Total", "C2"),
            new("saldoActual", "Saldo actual", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.ProveedorNombre, i.OrdenesCount, i.TotalMonto, i.SaldoActual
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "resumen-proveedores",
            Metadata.ReportTitle ?? "Resumen de proveedores",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One supplier row of the suppliers summary report.</summary>
public sealed record ResumenProveedoresReportItem(
    Guid ProveedorId,
    string ProveedorNombre,
    int OrdenesCount,
    decimal TotalMonto,
    decimal SaldoActual);
