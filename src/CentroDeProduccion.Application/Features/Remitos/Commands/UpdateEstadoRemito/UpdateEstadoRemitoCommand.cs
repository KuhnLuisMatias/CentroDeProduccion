using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.UpdateEstadoRemito;

public sealed record UpdateEstadoRemitoCommand(
    Guid RemitoId,
    EstadoRemito Estado,
    byte[] RowVersion);