namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Query for the cost report per product (recipe), optionally filtered by a single recipe and a
/// date range.
/// </summary>
public sealed record GetCostoProductoReportQuery(
    Guid? ProductoId = null,
    DateTime? From = null,
    DateTime? To = null);
