namespace CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;

public sealed record RegistrarConteoCommand(
    Guid InventarioSesionId,
    Guid ConteoId,
    decimal CantidadContada,
    string? Observaciones);
