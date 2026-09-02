using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the weekly matrix report: one row per article with its quantity per weekday slot,
/// plus per-day totals. A grand-total row ("TOTAL") is appended in <see cref="ToReportTable"/>
/// so exports keep the flat shape required by the generic DataTable.
/// </summary>
public sealed record GetMatrizSemanalReportDto(
    IReadOnlyList<MatrizSemanalReportItem> Items,
    MatrizSemanalTotales Totales,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("articulo", "Artículo"),
            new("lunes", "Lunes"),
            new("martes", "Martes"),
            new("miercoles", "Miércoles"),
            new("jueves", "Jueves"),
            new("viernes", "Viernes"),
            new("sabado", "Sábado"),
            new("domingo", "Domingo"),
            new("total", "Total")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Articulo, i.Lunes, i.Martes, i.Miercoles, i.Jueves, i.Viernes, i.Sabado, i.Domingo, i.Total
            }))
            .ToList();

        rows.Add(new ReportRow(new object?[]
        {
            "TOTAL", Totales.Lunes, Totales.Martes, Totales.Miercoles, Totales.Jueves,
            Totales.Viernes, Totales.Sabado, Totales.Domingo, Totales.TotalGeneral
        }));

        return new ReportTable(
            Metadata.ReportType ?? "pedidos-matriz",
            Metadata.ReportTitle ?? "Pedidos - resumen semanal",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One article row: quantity sent on each weekday slot across the range.</summary>
public sealed record MatrizSemanalReportItem(
    string Articulo,
    decimal Lunes,
    decimal Martes,
    decimal Miercoles,
    decimal Jueves,
    decimal Viernes,
    decimal Sabado,
    decimal Domingo,
    decimal Total);

/// <summary>Column totals across all articles for each weekday slot.</summary>
public sealed record MatrizSemanalTotales(
    decimal Lunes,
    decimal Martes,
    decimal Miercoles,
    decimal Jueves,
    decimal Viernes,
    decimal Sabado,
    decimal Domingo,
    decimal TotalGeneral);
