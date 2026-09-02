namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Query for the production-by-recipe report, optionally filtered by a single recipe and a date
/// range.
/// </summary>
public sealed record GetProduccionProductoReportQuery(
    Guid? RecetaId = null,
    DateTime? From = null,
    DateTime? To = null);
