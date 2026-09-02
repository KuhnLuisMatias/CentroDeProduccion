namespace CentroDeProduccion.Application.Features.Auth.Commands.Bootstrap;

public sealed record BootstrapCommand(
    string Nombre,
    string Apellido,
    string Email,
    string Password);
