namespace CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;

public sealed record EditarInsumosProduccionCommand(
    Guid ProduccionId,
    IReadOnlyList<LineaInsumoDto> Lineas);

public sealed record LineaInsumoDto(
    Guid InsumoId,
    decimal Cantidad,
    string? Observaciones);

public sealed record EditarInsumosProduccionResponse(Guid ProduccionId, int CantidadLineas);
