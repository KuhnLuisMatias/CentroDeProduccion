namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the sales-by-period report for a date range, grouped by day, week or month.
/// </summary>
public sealed record GetVentasPeriodoReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    string? Agrupacion = "dia");
