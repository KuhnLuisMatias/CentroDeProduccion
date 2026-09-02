using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the returns report. Return value is derived from the originating delivery note line
/// prices, matching each returned finished product to its remito line.
/// </summary>
public class GetDevolucionesReportQueryHandler
{
    private readonly IDevolucionRepository _devolucionRepository;

    public GetDevolucionesReportQueryHandler(IDevolucionRepository devolucionRepository)
    {
        _devolucionRepository = devolucionRepository;
    }

    public async Task<Result<GetDevolucionesReportDto>> HandleAsync(
        GetDevolucionesReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? new DateTime(today.Year, today.Month, 1);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetDevolucionesReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var devoluciones = await _devolucionRepository.GetByFiltersAsync(null, query.BarId, from, to, ct);

        var items = devoluciones
            .Select(d => new DevolucionesReportItem(
                d.Id,
                d.Fecha,
                d.Remito?.BarId ?? Guid.Empty,
                d.Remito?.Bar?.Nombre ?? string.Empty,
                d.RemitoId,
                d.Remito?.NumeroRemito ?? 0,
                Math.Round(d.Lineas.Sum(l => l.Cantidad), 2),
                Math.Round(ComputeTotal(d), 2)))
            .OrderByDescending(i => i.Fecha)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.BarId.HasValue ? $"Bar: {query.BarId.Value}" : null,
            "devoluciones",
            "Devoluciones");

        return Result.Success(new GetDevolucionesReportDto(items, metadata));
    }

    private static decimal ComputeTotal(Devolucion devolucion)
    {
        var remitoLineas = devolucion.Remito?.Lineas ?? new List<RemitoLinea>();
        return devolucion.Lineas.Sum(l =>
        {
            var remitoLinea = remitoLineas.FirstOrDefault(rl => rl.ProductoTerminadoId == l.ProductoTerminadoId);
            return l.Cantidad * (remitoLinea?.PrecioUnitario ?? 0m);
        });
    }
}
