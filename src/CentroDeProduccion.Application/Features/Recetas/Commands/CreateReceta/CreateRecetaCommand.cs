using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
/// <summary>
/// One ingredient line of a new recipe: either a direct insumo (<see cref="InsumoId"/>) or a
/// sub-recipe (<see cref="RecetaOrigenId"/>) for BOM recursion. Exactly one must be set.
/// </summary>
public sealed record RecetaInsumoDto(
    Guid? InsumoId,
    Guid? RecetaOrigenId,
    decimal CantidadNecesaria,
    Guid UnidadMedidaId,
    string? Observaciones);

public sealed record CreateRecetaCommand(
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid? UnidadMedidaId,
    string? Descripcion,
    IReadOnlyList<RecetaInsumoDto> Insumos);
