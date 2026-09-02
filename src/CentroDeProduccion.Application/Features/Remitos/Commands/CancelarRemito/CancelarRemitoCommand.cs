namespace CentroDeProduccion.Application.Features.Remitos.Commands.CancelarRemito;

public sealed record CancelarRemitoCommand(
    Guid RemitoId,
    byte[] RowVersion);