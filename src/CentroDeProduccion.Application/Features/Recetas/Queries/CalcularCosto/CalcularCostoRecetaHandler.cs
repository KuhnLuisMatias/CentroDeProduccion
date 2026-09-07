using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Features.Recetas.Queries.CalcularCosto;

/// <summary>
/// Resolves a recipe's cost through <see cref="RecetaCostoResolver"/> — the single source of
/// truth for BOM costing (real last-confirmed-production unit costs for sub-recipes, live BOM
/// fallback otherwise). Uses the last purchase price for direct insumos (spec §3.6).
/// </summary>
public class CalcularCostoRecetaHandler
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public CalcularCostoRecetaHandler(
        IRecetaRepository recetaRepository,
        RecetaCostoResolver recetaCostoResolver)
    {
        _recetaRepository = recetaRepository;
        _recetaCostoResolver = recetaCostoResolver;
    }

    public async Task<Result<CalcularCostoRecetaResponse>> HandleAsync(CalcularCostoRecetaQuery query, CancellationToken cancellationToken = default)
    {
        var receta = await _recetaRepository.GetByIdWithDetallesAsync(query.RecetaId, cancellationToken);
        if (receta == null)
        {
            return Result.Failure<CalcularCostoRecetaResponse>(
                Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada"));
        }

        var resultado = await _recetaCostoResolver.CalcularAsync(receta, cancellationToken);

        return new CalcularCostoRecetaResponse(
            receta.Id,
            receta.Nombre,
            resultado.CostoInsumos,
            resultado.CostoUnitario,
            resultado.CicloDetectado);
    }
}
