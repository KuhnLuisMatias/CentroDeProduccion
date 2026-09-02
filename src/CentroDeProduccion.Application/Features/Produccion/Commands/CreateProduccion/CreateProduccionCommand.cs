namespace CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;

public sealed record CreateProduccionCommand(
    Guid RecetaId,
    string? Observaciones);
