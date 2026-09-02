namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the sales-by-bar report, optionally filtered by bar and date range.
/// </summary>
public sealed record GetVentasPorBarReportQuery(
    Guid? BarId = null,
    DateTime? From = null,
    DateTime? To = null);
