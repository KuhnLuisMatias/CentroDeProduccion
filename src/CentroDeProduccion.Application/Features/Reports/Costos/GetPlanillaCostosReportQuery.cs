namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Query for the cost worksheet ("planilla de costos") of a single recipe: one row per BOM line
/// with its resolved unit price and subtotal, replicating the per-product sheets of the
/// original PLANTILLA COSTOS.xlsx.
/// </summary>
public sealed record GetPlanillaCostosReportQuery(Guid RecetaId);
