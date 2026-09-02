namespace CentroDeProduccion.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshTokenResponse(
    string Token,
    string RefreshToken);
