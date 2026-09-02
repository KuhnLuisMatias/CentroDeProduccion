using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the valued insumo stock report: current stock, last purchase price and total
/// value per insumo, plus the overall <see cref="TotalValorizado"/>.
/// </summary>
public sealed record GetStockInsumosValoradoReportDto(
    IReadOnlyList<StockInsumoValoradoReportItem> Items,
    decimal TotalValorizado,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("insumo", "Insumo"),
            new("unidadMedida", "Unidad"),
            new("stockActual", "Stock actual"),
            new("precioUltimaCompra", "Precio última compra", "C2"),
            new("valorTotal", "Valor total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Nombre, i.UnidadMedida, i.StockActual, i.PrecioUltimaCompra, i.ValorTotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-insumos-valorado",
            Metadata.ReportTitle ?? "Stock de insumos valorizado",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One insumo row of the valued stock report.</summary>
public sealed record StockInsumoValoradoReportItem(
    Guid InsumoId,
    string Nombre,
    string UnidadMedida,
    decimal StockActual,
    decimal PrecioUltimaCompra,
    decimal ValorTotal);
