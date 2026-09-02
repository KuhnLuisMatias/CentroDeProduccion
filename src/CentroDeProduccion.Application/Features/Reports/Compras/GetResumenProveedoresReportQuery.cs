namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Query for the suppliers summary report for a date range.
/// </summary>
public sealed record GetResumenProveedoresReportQuery(
    DateTime? From = null,
    DateTime? To = null);
