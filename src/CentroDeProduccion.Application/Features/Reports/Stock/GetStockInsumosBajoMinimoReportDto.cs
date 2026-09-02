using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the insumo stock-below-minimum report: each insumo whose current stock is at or
/// below its minimum, with the stock difference.
/// </summary>
public sealed record GetStockInsumosBajoMinimoReportDto(
    IReadOnlyList<StockInsumoBajoMinimoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("insumo", "Insumo"),
            new("stockActual", "Stock actual"),
            new("stockMinimo", "Stock mínimo"),
            new("diferenciaStock", "Diferencia stock")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Nombre, i.StockActual, i.StockMinimo, i.DiferenciaStock
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-insumos-bajo-minimo",
            Metadata.ReportTitle ?? "Stock de insumos bajo mínimo",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One insumo row of the below-minimum stock report.</summary>
public sealed record StockInsumoBajoMinimoReportItem(
    Guid InsumoId,
    string Nombre,
    decimal StockActual,
    decimal StockMinimo,
    decimal DiferenciaStock);
