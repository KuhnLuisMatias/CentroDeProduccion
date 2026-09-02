namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Query for the weekly matrix report: quantities of every delivered remito line pivoted by
/// article (finished product or insumo) × day of week, replicating the "RESUMEN SEMANAL" sheet
/// of the original QUE_MILA Excel. Columns are FIXED weekday slots (lunes..domingo): each
/// remito's date contributes to the column matching its <c>DayOfWeek</c>, regardless of actual
/// calendar dates. Ranges longer than 7 days accumulate all occurrences of each weekday.
/// </summary>
public sealed record GetMatrizSemanalReportQuery(
    Guid? BarId = null,
    DateTime From = default,
    DateTime To = default);
