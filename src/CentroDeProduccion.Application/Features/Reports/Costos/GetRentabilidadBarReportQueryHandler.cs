using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Builds the profitability report per bar. Revenue comes from delivered (Enviado) remito lines
/// grouped by bar; cost per bar is the sum of the costs of the finished products sold to that bar
/// (each product's cost attributed from its producing recipe).
/// </summary>
public class GetRentabilidadBarReportQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public GetRentabilidadBarReportQueryHandler(
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

    public async Task<Result<GetRentabilidadBarReportDto>> HandleAsync(
        GetRentabilidadBarReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetRentabilidadBarReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(query.BarId, EstadoRemito.Enviado, from, to, ct);

        var productosVendidos = remitos
            .SelectMany(r => r.Lineas)
            .Where(l => l.ProductoTerminadoId.HasValue)
            .Select(l => l.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();

        var costosPorProducto = await CargarCostosPorProductoAsync(productosVendidos, from, to, ct);

        var items = remitos
            .GroupBy(r => new { r.BarId, BarNombre = r.Bar?.Nombre ?? string.Empty })
            .Select(g =>
            {
                var ingresos = g.Sum(r => r.Lineas.Sum(l => l.PrecioUnitario * l.Cantidad));
                var costos = g.Sum(r => r.Lineas
                    .Where(l => l.ProductoTerminadoId.HasValue)
                    .Sum(l => costosPorProducto.TryGetValue(l.ProductoTerminadoId!.Value, out var c) ? c * l.Cantidad : 0m));
                var rentabilidad = ingresos - costos;
                var margen = ingresos > 0 ? rentabilidad / ingresos * 100m : 0m;
                return new RentabilidadBarReportItem(
                    g.Key.BarId,
                    g.Key.BarNombre,
                    Math.Round(ingresos, 2),
                    Math.Round(costos, 2),
                    Math.Round(rentabilidad, 2),
                    Math.Round(margen, 2));
            })
            .OrderByDescending(i => i.Ingresos)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.BarId.HasValue ? $"Bar: {query.BarId.Value}" : null,
            "rentabilidad-bar",
            "Rentabilidad por bar");

        return Result.Success(new GetRentabilidadBarReportDto(items, metadata));
    }

    private async Task<Dictionary<Guid, decimal>> CargarCostosPorProductoAsync(
        IReadOnlyList<Guid> productosVendidos, DateTime from, DateTime to, CancellationToken ct)
    {
        var costos = new Dictionary<Guid, decimal>();

        if (productosVendidos.Count == 0)
        {
            return costos;
        }

        var producciones = await _produccionRepository.GetByFiltersWithSalidasAsync(from, to, null, EstadoProduccion.Confirmada, ct);
        var costosPorReceta = producciones
            .GroupBy(p => p.RecetaId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.CostoTotal));
        var recetaPorProducto = new Dictionary<Guid, Guid>();

        foreach (var produccion in producciones)
        {
            foreach (var salida in produccion.Salidas)
            {
                recetaPorProducto.TryAdd(salida.ProductoTerminadoId, produccion.RecetaId);
            }
        }

        var recetasPorId = (await _recetaRepository.GetAllActiveAsync(ct)).ToDictionary(r => r.Id);

        foreach (var productoId in productosVendidos)
        {
            if (recetaPorProducto.TryGetValue(productoId, out var recetaId) &&
                costosPorReceta.TryGetValue(recetaId, out var costoProduccion))
            {
                costos[productoId] = costoProduccion;
            }
            else if (recetaPorProducto.TryGetValue(productoId, out recetaId) &&
                     recetasPorId.TryGetValue(recetaId, out var receta))
            {
                var costoReceta = await _recetaCostoResolver.CalcularAsync(receta, ct);
                costos[productoId] = costoReceta.CostoInsumos;
            }
            else
            {
                costos[productoId] = 0m;
            }
        }

        return costos;
    }
}
