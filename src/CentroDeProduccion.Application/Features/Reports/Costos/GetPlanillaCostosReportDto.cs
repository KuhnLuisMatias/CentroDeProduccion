using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Result of the cost worksheet report: recipe header data, one item per recipe line (insumo or
/// sub-recipe) with its resolved unit price and subtotal, plus the aggregate batch/unit costs.
/// </summary>
public sealed record GetPlanillaCostosReportDto(
    PlanillaCostosRecetaHeader Receta,
    IReadOnlyList<PlanillaCostosItem> Items,
    PlanillaCostosTotales Costos,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("referencia", "Referencia"),
            new("tipoLinea", "Tipo"),
            new("cantidadNecesaria", "Cantidad"),
            new("unidadMedida", "Unidad"),
            new("precioUnitario", "Precio unitario", "C2"),
            new("subtotal", "Subtotal", "C2"),
            new("observaciones", "Observaciones")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Referencia, i.TipoLinea, i.CantidadNecesaria, i.UnidadMedida,
                i.PrecioUnitario, i.Subtotal, i.Observaciones
            }))
            .ToList();

        rows.Add(new ReportRow(new object?[]
        {
            "TOTAL INSUMOS LOTE", string.Empty, string.Empty, string.Empty,
            string.Empty, Costos.CostoInsumosLote, $"Costo unitario: {Costos.CostoUnitario:N2}"
        }));

        return new ReportTable(
            Metadata.ReportType ?? "planilla-costos",
            Metadata.ReportTitle ?? $"Planilla de costos - {Receta.Nombre}",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>Recipe header block of the cost worksheet.</summary>
public sealed record PlanillaCostosRecetaHeader(
    Guid Id,
    string Nombre,
    string Categoria);

/// <summary>One recipe line of the cost worksheet.</summary>
public sealed record PlanillaCostosItem(
    string Referencia,
    string TipoLinea,
    decimal CantidadNecesaria,
    string UnidadMedida,
    decimal PrecioUnitario,
    decimal Subtotal,
    string? Observaciones);

/// <summary>Aggregate costs computed from the line subtotals.</summary>
public sealed record PlanillaCostosTotales(
    decimal CostoInsumosLote,
    decimal CostoUnitario);
