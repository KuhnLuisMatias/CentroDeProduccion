namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Query for the profitability report per finished product, optionally filtered by a single
/// finished product and a date range.
/// </summary>
public sealed record GetRentabilidadProductoReportQuery(
    Guid? ProductoId = null,
    DateTime? From = null,
    DateTime? To = null);
