namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Query for the profitability report per bar, optionally filtered by a single bar and a date
/// range.
/// </summary>
public sealed record GetRentabilidadBarReportQuery(
    Guid? BarId = null,
    DateTime? From = null,
    DateTime? To = null);
