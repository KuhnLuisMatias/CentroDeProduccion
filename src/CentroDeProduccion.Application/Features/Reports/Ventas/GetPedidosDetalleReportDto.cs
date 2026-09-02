using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the detailed orders report: one row per remito line plus the overall
/// <see cref="TotalGeneral"/> (sum of line subtotals), mirroring how the valued stock report
/// carries its grand total.
/// </summary>
public sealed record GetPedidosDetalleReportDto(
    IReadOnlyList<PedidosDetalleReportItem> Items,
    decimal TotalGeneral,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha"),
            new("numeroRemito", "Remito"),
            new("estado", "Estado"),
            new("barNombre", "Bar"),
            new("producto", "Producto"),
            new("tipoLinea", "Tipo"),
            new("cantidad", "Cantidad"),
            new("unidad", "Unidad"),
            new("precioUnitario", "Precio", "C2"),
            new("subtotal", "Total", "C2"),
            new("proveedor", "Proveedor"),
            new("lote", "Lote"),
            new("observaciones", "Observaciones")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.NumeroRemito, i.Estado, i.BarNombre, i.Producto, i.TipoLinea,
                i.Cantidad, i.Unidad, i.PrecioUnitario, i.Subtotal, i.Proveedor, i.Lote, i.Observaciones
            }))
            .ToList();

        rows.Add(new ReportRow(new object?[]
        {
            "TOTAL", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, TotalGeneral, string.Empty, string.Empty, null
        }));

        return new ReportTable(
            Metadata.ReportType ?? "pedidos-detalle",
            Metadata.ReportTitle ?? "Pedidos - detalle",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One remito line of the detailed orders report.</summary>
public sealed record PedidosDetalleReportItem(
    DateTime Fecha,
    int NumeroRemito,
    string Estado,
    string BarNombre,
    string Producto,
    string TipoLinea,
    decimal Cantidad,
    string Unidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    string Proveedor,
    string? Lote,
    string? Observaciones);
