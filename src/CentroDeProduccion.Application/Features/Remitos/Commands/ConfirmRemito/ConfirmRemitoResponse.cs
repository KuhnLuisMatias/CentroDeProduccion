using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;

public sealed record ConfirmRemitoResponse(
    Guid RemitoId,
    int NumeroRemito,
    EstadoRemito Estado,
    decimal Total,
    DateTime? FechaEnvio);