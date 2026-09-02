using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Security;

/// <summary>Issues short-lived JWT access tokens carrying the Rol claim.</summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(Usuario usuario);
}
