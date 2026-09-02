namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Query for the production-by-period report. <paramref name="Agrupacion"/> selects the
/// grouping granularity: "dia", "semana" or "mes" (default "dia").
/// </summary>
public sealed record GetProduccionPeriodoReportQuery(
    DateTime? From = null,
    DateTime? To = null,
    string Agrupacion = "dia");
