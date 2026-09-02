using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Features.Auth.Commands.Refresh;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// D4 rotation + reuse detection. The handler hashes the incoming RAW token to look it up and
/// must respond with a NEW raw token, not its hash. On the pre-fix code the login flow handed
/// out the hash, so every refresh double-hashed and failed with TOKEN_NOT_FOUND — these tests
/// pin the corrected contract.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly ITokenHasher _tokenHasher = new TokenHasher();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static IConfiguration BuildConfiguration(int expirationDays = 7) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:RefreshTokenExpirationDays"] = expirationDays.ToString()
        })
        .Build();

    private RefreshTokenCommandHandler CreateHandler(IConfiguration configuration) => new(
        _refreshTokenRepository, _usuarioRepository, _jwtTokenService, _tokenHasher, _unitOfWork, configuration);

    private static Usuario CreateUsuario(Guid id) => new()
    {
        Id = id,
        Email = "admin@centro.com",
        Nombre = "Admin",
        Apellido = "Centro",
        Rol = Rol.Administrador,
        Activo = true
    };

    [Fact]
    public async Task HandleAsync_WithRawToken_RotatesAndReturnsNewRawToken()
    {
        var rawToken = Guid.NewGuid().ToString();
        var usuario = CreateUsuario(Guid.NewGuid());
        var familiaId = Guid.NewGuid();
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(rawToken),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(7),
            FechaCreacion = DateTime.UtcNow,
            Revocado = false,
            FamiliaId = familiaId
        };

        _refreshTokenRepository.GetByTokenHashAsync(_tokenHasher.Hash(rawToken)).Returns(existingToken);
        _refreshTokenRepository.GetByFamiliaIdAsync(familiaId).Returns(new[] { existingToken });
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _jwtTokenService.GenerateAccessToken(usuario).Returns("new-access-token");
        RefreshToken? newStored = null;
        _ = _refreshTokenRepository.AddAsync(Arg.Do<RefreshToken>(rt => newStored = rt), Arg.Any<CancellationToken>());

        var result = await CreateHandler(BuildConfiguration())
            .HandleAsync(new RefreshTokenCommand(rawToken));

        result.IsSuccess.ShouldBeTrue();
        existingToken.Revocado.ShouldBeTrue();
        existingToken.MotivoRevocacion.ShouldBe("Rotation");
        newStored.ShouldNotBeNull();
        newStored.FamiliaId.ShouldBe(familiaId);
        newStored.ReemplazadoPorId.ShouldBe(existingToken.Id);
        newStored.Revocado.ShouldBeFalse();
        result.Value.Token.ShouldBe("new-access-token");
        result.Value.RefreshToken.ShouldNotBe(newStored.TokenHash);
        _tokenHasher.Hash(result.Value.RefreshToken).ShouldBe(newStored.TokenHash);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithRevokedRawToken_RevokesWholeFamily()
    {
        var rawToken = Guid.NewGuid().ToString();
        var usuario = CreateUsuario(Guid.NewGuid());
        var familiaId = Guid.NewGuid();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(rawToken),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(7),
            FechaCreacion = DateTime.UtcNow,
            Revocado = true,
            FamiliaId = familiaId
        };
        var sibling = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(Guid.NewGuid().ToString()),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(7),
            FechaCreacion = DateTime.UtcNow,
            Revocado = false,
            FamiliaId = familiaId
        };

        _refreshTokenRepository.GetByTokenHashAsync(_tokenHasher.Hash(rawToken)).Returns(revokedToken);
        _refreshTokenRepository.GetByFamiliaIdAsync(familiaId).Returns(new[] { revokedToken, sibling });

        var result = await CreateHandler(BuildConfiguration())
            .HandleAsync(new RefreshTokenCommand(rawToken));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("TOKEN_REUSE_DETECTED");
        sibling.Revocado.ShouldBeTrue();
        sibling.MotivoRevocacion.ShouldBe("Reuse detected");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}