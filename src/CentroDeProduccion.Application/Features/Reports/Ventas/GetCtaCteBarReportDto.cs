using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the bar current-account report: the running balance per movement plus the final
/// balance at the end of the range.
/// </summary>
public sealed record GetCtaCteBarReportDto(
    IReadOnlyList<CtaCteBarReportItem> Items,
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
            Metadata.ReportType ?? "cta-cte-bar",
            Metadata.ReportTitle ?? "Cuenta corriente del bar",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One movement row of the bar current-account report.</summary>
public sealed record CtaCteBarReportItem(
    DateTime Fecha,
    string Tipo,
    string? Referencia,
    decimal Monto,
    decimal Saldo);
