namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the bar current-account report for a bar and date range.
/// </summary>
public sealed record GetCtaCteBarReportQuery(
    Guid BarId,
    DateTime? From = null,
    DateTime? To = null);
