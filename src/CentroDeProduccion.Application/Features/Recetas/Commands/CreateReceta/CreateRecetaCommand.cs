using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
/// <summary>
/// One ingredient line of a new recipe: either a direct insumo (<see cref="InsumoId"/>) or a
/// sub-recipe (<see cref="RecetaOrigenId"/>) for BOM recursion. Exactly one must be set.
/// The line's unit is derived server-side (insumo's unidad de consumo / origen receta's unit);
/// the client never sends it.
/// </summary>
public sealed record RecetaInsumoDto(
    Guid? InsumoId,
    Guid? RecetaOrigenId,
    decimal CantidadNecesaria,
    string? Observaciones);

public sealed record CreateRecetaCommand(
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid? UnidadMedidaId,
    string? Descripcion,
    IReadOnlyList<RecetaInsumoDto> Insumos);

/// <summary>
/// Derives each recipe line's unit of measure server-side; the client-sent unit is not trusted.
/// Insumo lines take the insumo's unidad de consumo; sub-recipe lines take the origen
/// receta's resulting unit.
/// </summary>
internal static class RecetaLineaUnidades
{
    public static async Task<Result<IReadOnlyList<Guid>>> DerivarAsync(
        IInsumoRepository insumoRepository,
        IRecetaRepository recetaRepository,
        IReadOnlyList<RecetaInsumoDto> detalles,
        CancellationToken cancellationToken)
    {
        var insumoIds = detalles
            .Where(d => d.InsumoId.HasValue)
            .Select(d => d.InsumoId!.Value)
            .Distinct()
            .ToList();
        var insumosPorId = (await insumoRepository.GetByIdsAsync(insumoIds, cancellationToken))
            .ToDictionary(i => i.Id);

        var origenIds = detalles
            .Where(d => d.RecetaOrigenId.HasValue)
            .Select(d => d.RecetaOrigenId!.Value)
            .Distinct()
            .ToList();
        var origenesPorId = new Dictionary<Guid, Receta>();
        foreach (var origenId in origenIds)
        {
            var origen = await recetaRepository.GetByIdAsync(origenId, cancellationToken);
            if (origen is not null)
            {
                origenesPorId[origenId] = origen;
            }
        }

        var unidades = new List<Guid>(detalles.Count);
        foreach (var detalle in detalles)
        {
            if (detalle.InsumoId.HasValue)
            {
                if (!insumosPorId.TryGetValue(detalle.InsumoId.Value, out var insumo))
                {
                    return Result.Failure<IReadOnlyList<Guid>>(
                        Error.NotFound("INSUMO_NOT_FOUND", "Uno de los insumos de la receta no existe"));
                }
                unidades.Add(insumo.UnidadConsumoId);
            }
            else
            {
                if (!origenesPorId.TryGetValue(detalle.RecetaOrigenId!.Value, out var origen))
                {
                    return Result.Failure<IReadOnlyList<Guid>>(
                        Error.NotFound("RECETA_ORIGEN_NOT_FOUND", "Una de las sub-recetas de la receta no existe"));
                }
                if (origen.UnidadMedidaId is null)
                {
                    return Result.Failure<IReadOnlyList<Guid>>(
                        Error.Unexpected("RECETA_ORIGEN_SIN_UNIDAD",
                            $"La sub-receta \"{origen.Nombre}\" no tiene unidad de medida resultante"));
                }
                unidades.Add(origen.UnidadMedidaId.Value);
            }
        }

        return unidades;
    }
}
