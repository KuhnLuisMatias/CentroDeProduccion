namespace CentroDeProduccion.Application.Features.Auth.Commands.Register;

public sealed record RegisterResponse(
    Guid UsuarioId,
    string Email,
    string Nombre,
    string Apellido,
    string Token,
    string RefreshToken);
