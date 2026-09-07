using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Reports.Costos;

namespace CentroDeProduccion.Application.Common;

/// <summary>
/// PT cost policy: last confirmed production unit cost (real) when available; live recipe-BOM
/// estimate otherwise. All PT-cost consumers route through here.
/// </summary>
public class ProductoTerminadoCostoResolver
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IProduccionRepository _produccionRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public ProductoTerminadoCostoResolver(
        IRecetaRepository recetaRepository,
        IProduccionRepository produccionRepository,
        RecetaCostoResolver recetaCostoResolver)
    {
        _recetaRepository = recetaRepository;
        _produccionRepository = produccionRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    /// <summary>Unit cost of the product behind <paramref name="recetaId"/>: its last confirmed
    /// production's real unit cost when available, else the recipe BOM estimate; 0 when the
    /// product has no recipe (manually created) or the recipe is missing/cyclic.</summary>
    public async Task<decimal> CalcularPorRecetaAsync(Guid? recetaId, CancellationToken ct = default)
    {
        if (recetaId is null)
        {
            return 0m;
        }

        var costosReales = await _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(
            new[] { recetaId.Value }, ct) ?? new Dictionary<Guid, decimal>();
        return await ResolverConCostosAsync(recetaId.Value, costosReales, ct);
    }

    /// <summary>Batch variant keyed by receta id (skips nulls). One real-cost lookup for the
    /// whole set; ids missing from it fall back to the live BOM estimate.</summary>
    public async Task<IReadOnlyDictionary<Guid, decimal>> CalcularPorRecetasAsync(
        IEnumerable<Guid?> recetaIds, CancellationToken ct = default)
    {
        var ids = recetaIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        // ?? : NSubstitute defaults an unconfigured call to null; null simply means
        // "no known real costs" → full BOM fallback, which is the correct semantic.
        var costosReales = ids.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(ids, ct) ??
              new Dictionary<Guid, decimal>();

        var costos = new Dictionary<Guid, decimal>();
        foreach (var recetaId in ids)
        {
            costos[recetaId] = await ResolverConCostosAsync(recetaId, costosReales, ct);
        }

        return costos;
    }

    private async Task<decimal> ResolverConCostosAsync(
        Guid recetaId, IReadOnlyDictionary<Guid, decimal> costosReales, CancellationToken ct)
    {
        var receta = await _recetaRepository.GetByIdWithDetallesAsync(recetaId, ct);
        if (receta is null)
        {
            return 0m;
        }

        if (costosReales.TryGetValue(recetaId, out var costoReal))
        {
            return costoReal;
        }

        var resultado = await _recetaCostoResolver.CalcularAsync(receta, ct);
        return resultado.CicloDetectado ? 0m : resultado.CostoUnitario;
    }
}
