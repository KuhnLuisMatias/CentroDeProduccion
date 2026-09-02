namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Query for the input-price-evolution report, optionally filtered by input and date range.
/// </summary>
public sealed record GetEvolucionPreciosReportQuery(
    Guid? InsumoId = null,
    DateTime? From = null,
    DateTime? To = null);
