using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;

public sealed record CreateProduccionResponse(
    Guid Id,
    EstadoProduccion Estado);
