namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Query for the supplier current-account report for a supplier and date range.
/// </summary>
public sealed record GetCtaCteProveedorReportQuery(
    Guid ProveedorId,
    DateTime? From = null,
    DateTime? To = null);
