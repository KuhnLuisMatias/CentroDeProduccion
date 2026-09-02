using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Builds the profitability report per finished product. Revenue comes from delivered (Enviado)
/// remito lines; cost is attributed from the recipe that produced each product (its confirmed
/// productions' total cost, falling back to the recipe's standard cost).
/// </summary>
public class GetRentabilidadProductoReportQueryHandler
{
    private const string SinCostoObservacion = "sin costo registrado";

    private readonly IRemitoRepository _remitoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public GetRentabilidadProductoReportQueryHandler(
        IRemitoRepository remitoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IProduccionRepository produccionRepository,
        IRecetaRepository recetaRepository,
        RecetaCostoResolver recetaCostoResolver)
    {
        _remitoRepository = remitoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _produccionRepository = produccionRepository;
        _recetaRepository = recetaRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    public async Task<Result<GetRentabilidadProductoReportDto>> HandleAsync(
        GetRentabilidadProductoReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetRentabilidadProductoReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(null, EstadoRemito.Enviado, from, to, ct);

        var ingresosPorProducto = remitos
            .SelectMany(r => r.Lineas)
            .Where(l => l.ProductoTerminadoId.HasValue)
            .GroupBy(l => l.ProductoTerminadoId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(l => l.PrecioUnitario * l.Cantidad));

        if (query.ProductoId.HasValue)
        {
            ingresosPorProducto = ingresosPorProducto
                .Where(kv => kv.Key == query.ProductoId.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        var productos = await _productoTerminadoRepository.GetByIdsAsync(ingresosPorProducto.Keys.ToList(), ct);
        var productoPorId = productos.ToDictionary(p => p.Id, p => p.Nombre);

        var producciones = await _produccionRepository.GetByFiltersWithSalidasAsync(from, to, null, EstadoProduccion.Confirmada, ct);
        var costosPorReceta = CargarCostosPorReceta(producciones);
        var recetaPorProducto = CargarRecetaPorProducto(producciones);
        var recetasPorId = (await _recetaRepository.GetAllActiveAsync(ct)).ToDictionary(r => r.Id);

        var items = new List<RentabilidadProductoReportItem>();
        foreach (var kv in ingresosPorProducto)
        {
            var ingresos = kv.Value;
            string? observacion = null;
            decimal costos;

            if (recetaPorProducto.TryGetValue(kv.Key, out var recetaId) &&
                recetasPorId.TryGetValue(recetaId, out var receta))
            {
                if (costosPorReceta.TryGetValue(recetaId, out var costoProduccion))
                {
                    costos = costoProduccion;
                }
                else
                {
                    var costoReceta = await _recetaCostoResolver.CalcularAsync(receta, ct);
                    costos = costoReceta.CostoInsumos;
                }
            }
            else
            {
                costos = 0m;
                observacion = SinCostoObservacion;
            }

            var rentabilidad = ingresos - costos;
            var margen = ingresos > 0 ? rentabilidad / ingresos * 100m : 0m;

            items.Add(new RentabilidadProductoReportItem(
                kv.Key,
                productoPorId.TryGetValue(kv.Key, out var nombre) ? nombre : string.Empty,
                Math.Round(ingresos, 2),
                Math.Round(costos, 2),
                Math.Round(rentabilidad, 2),
                Math.Round(margen, 2),
                observacion));
        }

        items = items
            .OrderByDescending(i => i.Ingresos)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.ProductoId.HasValue ? $"Producto: {query.ProductoId.Value}" : null,
            "rentabilidad-producto",
            "Rentabilidad por producto");

        return Result.Success(new GetRentabilidadProductoReportDto(items, metadata));
    }

    private static Dictionary<Guid, decimal> CargarCostosPorReceta(IReadOnlyList<CentroDeProduccion.Domain.Entities.Produccion> producciones)
        => producciones
            .GroupBy(p => p.RecetaId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.CostoTotal));

    private static Dictionary<Guid, Guid> CargarRecetaPorProducto(IReadOnlyList<CentroDeProduccion.Domain.Entities.Produccion> producciones)
    {
        var resultado = new Dictionary<Guid, Guid>();

        foreach (var produccion in producciones)
        {
            foreach (var salida in produccion.Salidas)
            {
                resultado.TryAdd(salida.ProductoTerminadoId, produccion.RecetaId);
            }
        }

        return resultado;
    }
}
