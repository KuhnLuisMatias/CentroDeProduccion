using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the finished-product stock-movements report: one row per movement with quantity,
/// unit cost and subtotal.
/// </summary>
public sealed record GetStockPTMovimientosReportDto(
    IReadOnlyList<StockPTMovimientoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha"),
            new("producto", "Producto"),
            new("tipo", "Tipo"),
            new("cantidad", "Cantidad"),
            new("costoUnitario", "Costo unitario", "C2"),
            new("subtotal", "Subtotal", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.ProductoTerminadoNombre, i.Tipo.ToString(), i.Cantidad, i.CostoUnitario, i.Subtotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-pt-movimientos",
            Metadata.ReportTitle ?? "Movimientos de stock de productos terminados",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One movement row of the finished-product stock-movements report.</summary>
public sealed record StockPTMovimientoReportItem(
    DateTime Fecha,
    Guid ProductoTerminadoId,
    string ProductoTerminadoNombre,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    decimal CostoUnitario,
    decimal Subtotal);
