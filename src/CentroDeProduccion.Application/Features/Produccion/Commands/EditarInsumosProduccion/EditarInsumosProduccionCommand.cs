namespace CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;

public sealed record EditarInsumosProduccionCommand(
    Guid ProduccionId,
    IReadOnlyList<LineaInsumoDto> Lineas);

/// <summary>One consumption line: either a direct insumo (<see cref="InsumoId"/>) or a
/// sub-recipe consumption (<see cref="RecetaOrigenId"/>). Exactly one of the two must be set.</summary>
public sealed record LineaInsumoDto(
    Guid? InsumoId,
    Guid? RecetaOrigenId,
    decimal Cantidad,
    string? Observaciones);

public sealed record EditarInsumosProduccionResponse(Guid ProduccionId, int CantidadLineas);
