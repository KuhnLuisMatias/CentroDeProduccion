using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Recetas.Queries.CalcularCosto;

/// <summary>
/// Resolves a recipe's cost using <see cref="CostoService"/> (BOM recursion). Loads the recipe
/// tree and insumo prices eagerly (async), then hands in-memory lookups to the pure resolver.
/// Uses the last purchase price by default (spec §3.6); a per-recipe override can replace
/// <paramref name="obtenerPrecio"/> later.
/// </summary>
public class CalcularCostoRecetaHandler
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;

    public CalcularCostoRecetaHandler(
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
    }

    public async Task<Result<CalcularCostoRecetaResponse>> HandleAsync(CalcularCostoRecetaQuery query, CancellationToken cancellationToken = default)
    {
        var receta = await _recetaRepository.GetByIdWithDetallesAsync(query.RecetaId, cancellationToken);
        if (receta == null)
        {
            return Result.Failure<CalcularCostoRecetaResponse>(
                Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada"));
        }

        var recetas = new Dictionary<Guid, Receta>();
        var insumoIds = new HashSet<Guid>();
        await CargarArbolAsync(receta, recetas, insumoIds, new HashSet<Guid>(), cancellationToken);

        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var precios = insumos.ToDictionary(i => i.Id, i => i.PrecioUltimaCompra);

        var resultado = CostoService.Calcular(
            receta,
            id => recetas.TryGetValue(id, out var r) ? r : null,
            id => precios.TryGetValue(id, out var p) ? p : 0);

        return new CalcularCostoRecetaResponse(
            receta.Id,
            receta.Nombre,
            resultado.CostoInsumos,
            resultado.CostoUnitario,
            resultado.CicloDetectado);
    }

    private async Task CargarArbolAsync(
        Receta receta,
        Dictionary<Guid, Receta> recetas,
        HashSet<Guid> insumoIds,
        HashSet<Guid> visitados,
        CancellationToken cancellationToken)
    {
        recetas[receta.Id] = receta;

        foreach (var detalle in receta.Insumos)
        {
            if (detalle.InsumoId.HasValue)
            {
                insumoIds.Add(detalle.InsumoId.Value);
            }
            else if (detalle.RecetaOrigenId.HasValue && visitados.Add(detalle.RecetaOrigenId.Value))
            {
                var subReceta = await _recetaRepository.GetByIdWithDetallesAsync(detalle.RecetaOrigenId.Value, cancellationToken);
                if (subReceta is not null)
                {
                    await CargarArbolAsync(subReceta, recetas, insumoIds, visitados, cancellationToken);
                }
            }
        }
    }
}
