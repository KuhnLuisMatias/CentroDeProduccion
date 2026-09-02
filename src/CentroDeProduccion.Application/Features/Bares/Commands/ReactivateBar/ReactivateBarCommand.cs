namespace CentroDeProduccion.Application.Features.Bares.Commands.ReactivateBar;

public sealed record ReactivateBarCommand(
    Guid Id,
    byte[] RowVersion);