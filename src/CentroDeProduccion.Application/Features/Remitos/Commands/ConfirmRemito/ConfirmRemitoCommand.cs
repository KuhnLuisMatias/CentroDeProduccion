namespace CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;

public sealed record ConfirmRemitoCommand(
    Guid RemitoId,
    byte[] RowVersion);