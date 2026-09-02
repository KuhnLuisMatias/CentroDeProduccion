using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Result of the returns report: one row per return with the originating delivery note, the total
/// returned quantity and the total value of the returned goods.
/// </summary>
public sealed record GetDevolucionesReportDto(
    IReadOnlyList<DevolucionesReportItem> Items,
    ReportMetadata Metadata)
{
    public ReportTable ToReportTable()
    {
        var columns = new List<ReportColumn>
        {
            new("fecha", "Fecha", "dd/MM/yyyy"),
            new("bar", "Bar"),
            new("numeroRemito", "N° remito"),
            new("cantidadDevuelta", "Cantidad devuelta"),
            new("totalDevolucion", "Total", "C2")
        };

        var rows = Items
            .Select(i => new ReportRow(new object?[]
            {
                i.Fecha, i.BarNombre, i.NumeroRemito, i.CantidadDevuelta, i.TotalDevolucion
            }))
            .ToList();

        return new ReportTable(
            Metadata.ReportType ?? "devoluciones",
            Metadata.ReportTitle ?? "Devoluciones",
            Metadata,
            columns,
            rows);
    }
}

/// <summary>One return row of the returns report.</summary>
public sealed record DevolucionesReportItem(
    Guid DevolucionId,
    DateTime Fecha,
    Guid BarId,
    string BarNombre,
    Guid RemitoId,
    int NumeroRemito,
    decimal CantidadDevuelta,
    decimal TotalDevolucion);
