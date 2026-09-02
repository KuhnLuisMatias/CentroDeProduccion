using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CentroDeProduccion.Application.Features.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler
{
    private const double DefaultRefreshTokenExpirationDays = 7;

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUsuarioRepository usuarioRepository,
        IJwtTokenService jwtTokenService,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _usuarioRepository = usuarioRepository;
        _jwtTokenService = jwtTokenService;
        _tokenHasher = tokenHasher;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    private double RefreshTokenExpirationDays()
    {
        var value = _configuration.GetSection("Jwt")["RefreshTokenExpirationDays"];
        return double.TryParse(value, out var days) ? days : DefaultRefreshTokenExpirationDays;
    }

    public async Task<Result<RefreshTokenResponse>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenHasher.Hash(command.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken == null)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.NotFound("TOKEN_NOT_FOUND", "Token de refresco no encontrado"));
        }

        if (existingToken.Revocado)
        {
            // Reuse detected — revoke entire family
            var familyTokens = await _refreshTokenRepository.GetByFamiliaIdAsync(existingToken.FamiliaId, cancellationToken);
            foreach (var familyToken in familyTokens)
            {
                familyToken.Revocado = true;
                familyToken.FechaRevocacion = DateTime.UtcNow;
                familyToken.MotivoRevocacion = "Reuse detected";
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("TOKEN_REUSE_DETECTED", "Se detectó reuso de token. Sesión revocada."));
        }

        if (existingToken.FechaExpiracion < DateTime.UtcNow)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("TOKEN_EXPIRED", "Token de refresco expirado"));
        }

        var usuario = await _usuarioRepository.GetByIdAsync(existingToken.UsuarioId, cancellationToken);
        if (usuario == null || !usuario.Activo)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("USER_INACTIVE", "Usuario no encontrado o inactivo"));
        }

        // Rotate: revoke old, issue new
        existingToken.Revocado = true;
        existingToken.FechaRevocacion = DateTime.UtcNow;
        existingToken.MotivoRevocacion = "Rotation";

        var tokenCrudo = Guid.NewGuid().ToString();
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(tokenCrudo),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays()),
            FechaCreacion = DateTime.UtcNow,
            Revocado = false,
            FamiliaId = existingToken.FamiliaId, // Same family for reuse detection
            ReemplazadoPorId = existingToken.Id
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(usuario);

        return new RefreshTokenResponse(accessToken, tokenCrudo);
    }
}
