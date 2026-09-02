namespace CentroDeProduccion.Application.Features.Bares.Commands.DeleteBar;

public sealed record DeleteBarCommand(
    Guid Id,
    byte[] RowVersion);