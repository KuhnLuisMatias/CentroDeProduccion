using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Reports.Costos;

namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Resolves a finished product's unit cost on the fly from its recipe's BOM at current
/// insumo prices (<see cref="RecetaCostoResolver"/>). ProductoTerminado no longer stores
/// CostoUnitario: the cost always reflects the latest purchase prices and can never go stale.
/// </summary>
public class ProductoTerminadoCostoResolver
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public ProductoTerminadoCostoResolver(IRecetaRepository recetaRepository, RecetaCostoResolver recetaCostoResolver)
    {
        _recetaRepository = recetaRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    /// <summary>Cost per lote of the recipe behind <paramref name="recetaId"/>; 0 when the
    /// product has no recipe (manually created) or the recipe is missing/cyclic.</summary>
    public async Task<decimal> CalcularPorRecetaAsync(Guid? recetaId, CancellationToken ct = default)
    {
        if (recetaId is null)
        {
            return 0m;
        }

        var receta = await _recetaRepository.GetByIdWithDetallesAsync(recetaId.Value, ct);
        if (receta is null)
        {
            return 0m;
        }

        var resultado = await _recetaCostoResolver.CalcularAsync(receta, ct);
        return resultado.CicloDetectado ? 0m : resultado.CostoUnitario;
    }

    /// <summary>Batch variant keyed by receta id (skips nulls).</summary>
    public async Task<IReadOnlyDictionary<Guid, decimal>> CalcularPorRecetasAsync(
        IEnumerable<Guid?> recetaIds, CancellationToken ct = default)
    {
        var costos = new Dictionary<Guid, decimal>();
        foreach (var recetaId in recetaIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            costos[recetaId] = await CalcularPorRecetaAsync(recetaId, ct);
        }

        return costos;
    }
}
