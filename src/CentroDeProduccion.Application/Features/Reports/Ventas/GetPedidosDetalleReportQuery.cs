namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the detailed orders ("pedidos") report: one flat row per delivered remito line,
/// replicating the daily sheets of the original QUE_MILA Excel (producto, cantidad, unidad,
/// precio, proveedor, total, observaciones).
/// </summary>
public sealed record GetPedidosDetalleReportQuery(
    Guid? BarId = null,
    DateTime? From = null,
    DateTime? To = null);
