using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the valued finished-product stock report: current stock, unit cost and total value
/// per finished product, plus the overall <see cref="TotalValorizado"/>.
/// </summary>
public sealed record GetStockPTValoradoReportDto(
    IReadOnlyList<StockPTValoradoReportItem> Items,
    decimal TotalValorizado,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("producto", "Producto"),
            new("stockActual", "Stock actual"),
            new("costoUnitario", "Costo unitario", "C2"),
            new("valorTotal", "Valor total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Nombre, i.StockActual, i.CostoUnitario, i.ValorTotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-pt-valorado",
            Metadata.ReportTitle ?? "Stock de productos terminados valorizado",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One finished-product row of the valued stock report.</summary>
public sealed record StockPTValoradoReportItem(
    Guid ProductoTerminadoId,
    string Nombre,
    decimal StockActual,
    decimal CostoUnitario,
    decimal ValorTotal);
