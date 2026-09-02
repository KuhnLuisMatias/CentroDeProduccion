using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the products-nearing-expiration report: one row per finished product expiring within
/// the horizon, with the days remaining.
/// </summary>
public sealed record GetStockPTProximosAVencerReportDto(
    IReadOnlyList<StockPTProximoAVencerReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("producto", "Producto"),
            new("stockActual", "Stock actual"),
            new("fechaVencimiento", "Fecha vencimiento"),
            new("diasParaVencer", "Días para vencer")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Nombre, i.StockActual, i.FechaVencimiento, i.DiasParaVencer
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-pt-proximos-a-vencer",
            Metadata.ReportTitle ?? "Productos terminados próximos a vencer",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One finished-product row of the nearing-expiration report.</summary>
public sealed record StockPTProximoAVencerReportItem(
    Guid ProductoTerminadoId,
    string Nombre,
    decimal StockActual,
    DateTime FechaVencimiento,
    int DiasParaVencer);
