namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the returns report, optionally filtered by bar and date range.
/// </summary>
public sealed record GetDevolucionesReportQuery(
    Guid? BarId = null,
    DateTime? From = null,
    DateTime? To = null);
