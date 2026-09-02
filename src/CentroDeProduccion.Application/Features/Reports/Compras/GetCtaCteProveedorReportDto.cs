using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Result of the supplier current-account report: the running balance per movement plus the final
/// balance at the end of the range.
/// </summary>
public sealed record GetCtaCteProveedorReportDto(
    IReadOnlyList<CtaCteProveedorReportItem> Items,
    ReportMetadata Metadata,
    decimal SaldoFinal)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha", "dd/MM/yyyy"),
            new("tipo", "Tipo"),
            new("referencia", "Referencia"),
            new("monto", "Monto", "C2"),
            new("saldo", "Saldo", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.Tipo, i.Referencia, i.Monto, i.Saldo
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "cta-cte-proveedor",
            Metadata.ReportTitle ?? "Cuenta corriente del proveedor",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One movement row of the supplier current-account report.</summary>
public sealed record CtaCteProveedorReportItem(
    DateTime Fecha,
    string Tipo,
    string? Referencia,
    decimal Monto,
    decimal Saldo);
