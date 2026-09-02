using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Result of the insumo stock-movements report: one row per movement with quantity, unit cost
/// and subtotal.
/// </summary>
public sealed record GetStockInsumosMovimientosReportDto(
    IReadOnlyList<StockInsumoMovimientoReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha"),
            new("insumo", "Insumo"),
            new("tipo", "Tipo"),
            new("cantidad", "Cantidad"),
            new("costoUnitario", "Costo unitario", "C2"),
            new("subtotal", "Subtotal", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.InsumoNombre, i.Tipo.ToString(), i.Cantidad, i.CostoUnitario, i.Subtotal
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "stock-insumos-movimientos",
            Metadata.ReportTitle ?? "Movimientos de stock de insumos",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One movement row of the insumo stock-movements report.</summary>
public sealed record StockInsumoMovimientoReportItem(
    DateTime Fecha,
    Guid InsumoId,
    string InsumoNombre,
    TipoMovimientoStock Tipo,
    decimal Cantidad,
    decimal CostoUnitario,
    decimal Subtotal);
