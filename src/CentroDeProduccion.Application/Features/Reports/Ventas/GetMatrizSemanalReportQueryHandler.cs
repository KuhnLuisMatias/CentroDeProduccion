using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the weekly matrix report: pivots delivered (Enviado) remito line quantities by article
/// (finished product or insumo) × day of week. Day columns are FIXED weekday slots
/// (lunes..domingo) filled by matching each remito date's <see cref="DayOfWeek"/>, so the grid
/// stays flat for the generic export pipeline; ranges spanning more than 7 days accumulate every
/// occurrence of each weekday (users are expected to select ranges of up to one week).
/// </summary>
public class GetMatrizSemanalReportQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;

    public GetMatrizSemanalReportQueryHandler(
        IRemitoRepository remitoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IInsumoRepository insumoRepository)
    {
        _remitoRepository = remitoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _insumoRepository = insumoRepository;
    }

    public async Task<Result<GetMatrizSemanalReportDto>> HandleAsync(
        GetMatrizSemanalReportQuery query, CancellationToken ct = default)
    {
        if (query.From > query.To)
        {
            return Result.Failure<GetMatrizSemanalReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(query.BarId, EstadoRemito.Enviado, query.From, query.To, ct);

        // [articulo][weekdayIndex] where 0 = lunes .. 6 = domingo.
        var acumulado = new Dictionary<string, decimal[]>(StringComparer.OrdinalIgnoreCase);
        var orden = new List<string>();

        void Acumular(string articulo, DateTime fecha, decimal cantidad)
        {
            if (!acumulado.TryGetValue(articulo, out var dias))
            {
                dias = new decimal[7];
                acumulado[articulo] = dias;
                orden.Add(articulo);
            }

            dias[IndiceDia(fecha)] += cantidad;
        }

        foreach (var x in remitos.SelectMany(r => r.Lineas.Select(l => new { Remito = r, Linea = l })))
        {
            if (x.Linea.ProductoTerminadoId.HasValue)
            {
                Acumular($"PT:{x.Linea.ProductoTerminadoId.Value}", x.Remito.Fecha, x.Linea.Cantidad);
            }
            else if (x.Linea.InsumoId.HasValue)
            {
                Acumular($"INS:{x.Linea.InsumoId.Value}", x.Remito.Fecha, x.Linea.Cantidad);
            }
        }

        var productoIds = orden.Where(a => a.StartsWith("PT:", StringComparison.Ordinal))
            .Select(a => Guid.Parse(a[3..]))
            .ToList();
        var insumoIds = orden.Where(a => a.StartsWith("INS:", StringComparison.Ordinal))
            .Select(a => Guid.Parse(a[4..]))
            .ToList();

        var nombresPorClave = (await _productoTerminadoRepository.GetByIdsAsync(productoIds, ct))
            .ToDictionary(p => $"PT:{p.Id}", p => p.Nombre);
        foreach (var insumo in await _insumoRepository.GetByIdsAsync(insumoIds, ct))
        {
            nombresPorClave[$"INS:{insumo.Id}"] = insumo.Nombre;
        }

        var items = new List<MatrizSemanalReportItem>();
        var totales = new decimal[7];
        decimal totalGeneral = 0m;

        foreach (var clave in orden.OrderBy(c => nombresPorClave.GetValueOrDefault(c, c)))
        {
            var dias = acumulado[clave];
            for (var i = 0; i < 7; i++)
            {
                totales[i] += dias[i];
            }

            var totalArticulo = dias.Sum();
            totalGeneral += totalArticulo;

            items.Add(new MatrizSemanalReportItem(
                nombresPorClave.GetValueOrDefault(clave, clave),
                Math.Round(dias[0], 2),
                Math.Round(dias[1], 2),
                Math.Round(dias[2], 2),
                Math.Round(dias[3], 2),
                Math.Round(dias[4], 2),
                Math.Round(dias[5], 2),
                Math.Round(dias[6], 2),
                Math.Round(totalArticulo, 2)));
        }

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            query.From,
            query.To,
            query.BarId.HasValue ? $"Bar: {query.BarId.Value}" : null,
            "pedidos-matriz",
            "Pedidos - resumen semanal");

        return Result.Success(new GetMatrizSemanalReportDto(
            items,
            new MatrizSemanalTotales(
                Math.Round(totales[0], 2),
                Math.Round(totales[1], 2),
                Math.Round(totales[2], 2),
                Math.Round(totales[3], 2),
                Math.Round(totales[4], 2),
                Math.Round(totales[5], 2),
                Math.Round(totales[6], 2),
                Math.Round(totalGeneral, 2)),
            metadata));
    }

    /// <summary>Maps a date to its fixed weekday slot: 0 = lunes .. 6 = domingo.</summary>
    private static int IndiceDia(DateTime fecha) => ((int)fecha.DayOfWeek + 6) % 7;
}
