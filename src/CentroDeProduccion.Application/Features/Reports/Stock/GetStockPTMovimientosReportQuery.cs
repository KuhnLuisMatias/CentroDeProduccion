namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Query for the finished-product stock-movements report, optionally filtered by date range and
/// a single finished product.
/// </summary>
public sealed record GetStockPTMovimientosReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    Guid? ProductoTerminadoId = null);
