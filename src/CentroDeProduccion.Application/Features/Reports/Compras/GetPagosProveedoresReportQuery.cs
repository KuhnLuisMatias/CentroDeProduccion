namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Query for the supplier-payments report, optionally filtered by supplier and date range.
/// </summary>
public sealed record GetPagosProveedoresReportQuery(
    Guid? ProveedorId = null,
    DateTime? From = null,
    DateTime? To = null);
