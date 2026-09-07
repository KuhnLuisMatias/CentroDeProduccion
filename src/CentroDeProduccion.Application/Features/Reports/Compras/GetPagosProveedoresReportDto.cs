using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Result of the supplier-payments report: one row per payment plus a summary of the total
/// paid per payment method over the period.
/// </summary>
public sealed record GetPagosProveedoresReportDto(
    IReadOnlyList<PagosProveedoresReportItem> Items,
    IReadOnlyDictionary<string, decimal> TotalesPorMedio,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha", "dateOnly"),
            new("proveedorNombre", "Proveedor"),
            new("monto", "Monto", "C2"),
            new("medios", "Medios de pago"),
            new("referencia", "Referencia"),
            new("observaciones", "Observaciones")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.ProveedorNombre, i.Monto, i.Medios, i.Referencia, i.Observaciones
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "pagos-proveedores",
            Metadata.ReportTitle ?? "Pagos a proveedores",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One payment row of the supplier-payments report.</summary>
public sealed record PagosProveedoresReportItem(
    Guid Id,
    DateTime Fecha,
    Guid ProveedorId,
    string ProveedorNombre,
    decimal Monto,
    string Medios,
    string? Referencia,
    Guid? FacturaId,
    string? Observaciones);
