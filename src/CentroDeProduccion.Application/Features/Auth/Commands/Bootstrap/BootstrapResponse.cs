namespace CentroDeProduccion.Application.Features.Auth.Commands.Bootstrap;

public sealed record BootstrapResponse(
    Guid UsuarioId,
    string Email,
    string Nombre,
    string Apellido);
