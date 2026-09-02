namespace CentroDeProduccion.Application.Features.Produccion.Commands.CancelProduccion;

public sealed record CancelProduccionCommand(Guid ProduccionId, string? Motivo);

public sealed record CancelProduccionResponse(
    Guid ProduccionId,
    Domain.Enums.EstadoProduccion Estado);
