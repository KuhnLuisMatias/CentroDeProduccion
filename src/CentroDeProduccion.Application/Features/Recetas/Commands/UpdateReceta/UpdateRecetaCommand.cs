using CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.UpdateReceta;

public sealed record UpdateRecetaCommand(
    Guid Id,
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid? UnidadMedidaId,
    string? Descripcion,
    EstadoReceta Estado,
    IReadOnlyList<RecetaInsumoDto> Insumos);
