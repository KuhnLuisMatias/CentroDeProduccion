using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Builds the cost report per product from confirmed productions. When a recipe has no confirmed
/// production in the range its standard recipe cost is used as a fallback (with an observation),
/// so every recipe is still reported.
/// </summary>
public class GetCostoProductoReportQueryHandler
{
    private const string SinCostoObservacion = "sin costo de produccion registrado";

    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public GetCostoProductoReportQueryHandler(
        IProduccionRepository produccionRepository,
        IRecetaRepository recetaRepository,
        RecetaCostoResolver recetaCostoResolver)
    {
        _produccionRepository = produccionRepository;
        _recetaRepository = recetaRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    public async Task<Result<GetCostoProductoReportDto>> HandleAsync(
        GetCostoProductoReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetCostoProductoReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var producciones = await _produccionRepository.GetByFiltersAsync(from, to, query.ProductoId, EstadoProduccion.Confirmada, ct);

        var produccionPorReceta = producciones
            .GroupBy(p => new { p.RecetaId, RecetaNombre = p.Receta?.Nombre ?? string.Empty })
            .ToDictionary(
                g => g.Key.RecetaId,
                g => new
                {
                    Nombre = g.Key.RecetaNombre,
                    Insumos = g.Sum(p => p.CostoTotalInsumos),
                    Total = g.Sum(p => p.CostoTotal),
                    Cantidad = g.Count()
                });

        var recetas = await _recetaRepository.GetAllActiveAsync(ct);

        var items = new List<CostoProductoReportItem>();
        foreach (var receta in recetas)
        {
            if (query.ProductoId.HasValue && receta.Id != query.ProductoId.Value)
            {
                continue;
            }

            if (produccionPorReceta.TryGetValue(receta.Id, out var prod))
            {
                items.Add(new CostoProductoReportItem(
                    receta.Id,
                    prod.Nombre,
                    Math.Round(prod.Insumos, 2),
                    Math.Round(prod.Total, 2),
                    prod.Cantidad,
                    null));
            }
            else
            {
                var costoReceta = await _recetaCostoResolver.CalcularAsync(receta, ct);
                items.Add(new CostoProductoReportItem(
                    receta.Id,
                    receta.Nombre,
                    Math.Round(costoReceta.CostoInsumos, 2),
                    Math.Round(costoReceta.CostoInsumos, 2),
                    0,
                    SinCostoObservacion));
            }
        }

        items = items
            .OrderByDescending(i => i.CostoTotal)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.ProductoId.HasValue ? $"Producto: {query.ProductoId.Value}" : null,
            "costo-producto",
            "Costo por producto");

        return Result.Success(new GetCostoProductoReportDto(items, metadata));
    }
}
