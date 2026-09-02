namespace CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;

public sealed record ConfirmInventarioSesionCommand(
    Guid InventarioSesionId,
    byte[] RowVersion);
