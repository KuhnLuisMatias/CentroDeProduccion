using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Reports.Costos;

/// <summary>
/// Resolves a recipe's standard cost (its "CostoReceta") for reports that need a fallback when no
/// production cost is available. Uses <see cref="CostoService"/> over the recipe BOM, mirroring the
/// canonical <c>CalcularCostoRecetaHandler</c>.
/// </summary>
public class RecetaCostoResolver
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;

    public RecetaCostoResolver(IRecetaRepository recetaRepository, IInsumoRepository insumoRepository)
    {
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
    }

    /// <summary>
    /// Computes the standard cost of <paramref name="receta"/> (recursively expanding sub-recipes).
    /// </summary>
    public async Task<CostoService.CostoResult> CalcularAsync(Receta receta, CancellationToken ct = default)
    {
        var recetas = new Dictionary<Guid, Receta>();
        var insumoIds = new HashSet<Guid>();
        await CargarArbolAsync(receta, recetas, insumoIds, new HashSet<Guid>(), ct);

        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, ct);
        var precios = insumos.ToDictionary(i => i.Id, i => i.PrecioUltimaCompra);

        return CostoService.Calcular(
            receta,
            id => recetas.TryGetValue(id, out var r) ? r : null,
            id => precios.TryGetValue(id, out var p) ? p : 0);
    }

    private async Task CargarArbolAsync(
        Receta receta,
        Dictionary<Guid, Receta> recetas,
        HashSet<Guid> insumoIds,
        HashSet<Guid> visitados,
        CancellationToken ct)
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
                var subReceta = await _recetaRepository.GetByIdWithDetallesAsync(detalle.RecetaOrigenId.Value, ct);
                if (subReceta is not null)
                {
                    await CargarArbolAsync(subReceta, recetas, insumoIds, visitados, ct);
                }
            }
        }
    }
}
