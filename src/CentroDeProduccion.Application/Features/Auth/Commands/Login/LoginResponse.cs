namespace CentroDeProduccion.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    Guid UsuarioId,
    string Email,
    string Nombre,
    string Apellido,
    string Rol,
    bool DebeCambiarPassword,
    string Token,
    string RefreshToken);
