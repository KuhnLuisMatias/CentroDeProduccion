using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Resolves a recipe's standard cost (its "CostoReceta") for reports that need a fallback when no
/// production cost is available. Sub-recipe lines are priced at the REAL unit cost of their last
/// confirmed production (<see cref="IProduccionRepository.GetLastConfirmedUnitCostsByRecetaAsync"/>);
/// sub-recipes never confirmed fall back to live BOM recursion via <see cref="CostoService"/>.
/// </summary>
public class RecetaCostoResolver
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProduccionRepository _produccionRepository;

    public RecetaCostoResolver(
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        IProduccionRepository produccionRepository)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _produccionRepository = produccionRepository;
    }

    /// <summary>
    /// Computes the standard cost of <paramref name="receta"/>: sub-recipes with a confirmed
    /// production use that real unit cost; the rest expand their BOM recursively.
    /// </summary>
    public async Task<CostoService.CostoResult> CalcularAsync(Receta receta, CancellationToken ct = default)
    {
        var recetas = new Dictionary<Guid, Receta>();
        var insumoIds = new HashSet<Guid>();
        var subrecetaIds = new HashSet<Guid>();
        await CargarArbolAsync(receta, recetas, insumoIds, subrecetaIds, ct);

        // ?? : NSubstitute defaults an unconfigured call to null; null simply means
        // "no known real costs" → full BOM fallback, which is the correct semantic.
        var costosReales = subrecetaIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await _produccionRepository.GetLastConfirmedUnitCostsByRecetaAsync(subrecetaIds, ct) ??
              new Dictionary<Guid, decimal>();

        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, ct);
        var precios = insumos.ToDictionary(i => i.Id, i => i.PrecioUltimaCompra);

        return CostoService.Calcular(
            receta,
            id => recetas.TryGetValue(id, out var r) ? r : null,
            id => precios.TryGetValue(id, out var p) ? p : 0,
            costosSubreceta: costosReales);
    }

    private async Task CargarArbolAsync(
        Receta receta,
        Dictionary<Guid, Receta> recetas,
        HashSet<Guid> insumoIds,
        HashSet<Guid> subrecetaIds,
        CancellationToken ct)
    {
        recetas[receta.Id] = receta;

        foreach (var detalle in receta.Insumos)
        {
            if (detalle.InsumoId.HasValue)
            {
                insumoIds.Add(detalle.InsumoId.Value);
            }
            else if (detalle.RecetaOrigenId.HasValue && subrecetaIds.Add(detalle.RecetaOrigenId.Value))
            {
                var subReceta = await _recetaRepository.GetByIdWithDetallesAsync(detalle.RecetaOrigenId.Value, ct);
                if (subReceta is not null)
                {
                    await CargarArbolAsync(subReceta, recetas, insumoIds, subrecetaIds, ct);
                }
            }
        }
    }
}
