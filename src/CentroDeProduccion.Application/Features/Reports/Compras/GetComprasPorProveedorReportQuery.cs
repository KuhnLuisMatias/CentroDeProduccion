namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Query for the purchases-by-supplier report, optionally filtered by supplier and date range.
/// </summary>
public sealed record GetComprasPorProveedorReportQuery(
    Guid? ProveedorId = null,
    DateTime? From = null,
    DateTime? To = null);
