using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;

public sealed record CreateRemitoResponse(
    Guid Id,
    int NumeroRemito,
    EstadoRemito Estado,
    decimal Total);