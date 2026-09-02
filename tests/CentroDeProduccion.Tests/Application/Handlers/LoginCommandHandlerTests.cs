using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Features.Auth.Commands.Login;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Security;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Guards the D4 refresh-token contract: login must hand the client the RAW token value
/// (returned once, never persisted) while only its SHA-256 hash is stored. Returning the
/// hash would double-hash on the next refresh and break the whole rotation chain.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly ITokenHasher _tokenHasher = new TokenHasher();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<LoginCommand> _validator = new LoginCommandValidator();

    private static IConfiguration BuildConfiguration(int expirationDays = 7) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:RefreshTokenExpirationDays"] = expirationDays.ToString()
        })
        .Build();

    private LoginCommandHandler CreateHandler(IConfiguration configuration) => new(
        _usuarioRepository, _passwordHasher, _jwtTokenService, _tokenHasher,
        _refreshTokenRepository, _unitOfWork, _validator, configuration);

    private static Usuario CreateUsuario() => new()
    {
        Id = Guid.NewGuid(),
        Email = "admin@centro.com",
        PasswordHash = "password-hash",
        Nombre = "Admin",
        Apellido = "Centro",
        Rol = Rol.Administrador,
        Activo = true
    };

    private void StubLogin(Usuario usuario)
    {
        _usuarioRepository.GetByEmailAsync(usuario.Email).Returns(usuario);
        _passwordHasher.Verify(usuario.PasswordHash, "Centro2026!").Returns(true);
        _jwtTokenService.GenerateAccessToken(usuario).Returns("access-token");
    }

    [Fact]
    public async Task HandleAsync_ReturnsRawRefreshToken_AndStoresItsHash()
    {
        var usuario = CreateUsuario();
        StubLogin(usuario);
        RefreshToken? stored = null;
        _ = _refreshTokenRepository.AddAsync(Arg.Do<RefreshToken>(rt => stored = rt), Arg.Any<CancellationToken>());

        var result = await CreateHandler(BuildConfiguration())
            .HandleAsync(new LoginCommand(usuario.Email, "Centro2026!"));

        result.IsSuccess.ShouldBeTrue();
        stored.ShouldNotBeNull();
        result.Value.RefreshToken.ShouldNotBe(stored.TokenHash);
        _tokenHasher.Hash(result.Value.RefreshToken).ShouldBe(stored.TokenHash);
    }

    [Fact]
    public async Task HandleAsync_UsesConfiguredRefreshTokenExpiration()
    {
        var usuario = CreateUsuario();
        StubLogin(usuario);
        RefreshToken? stored = null;
        _ = _refreshTokenRepository.AddAsync(Arg.Do<RefreshToken>(rt => stored = rt), Arg.Any<CancellationToken>());

        var result = await CreateHandler(BuildConfiguration(expirationDays: 3))
            .HandleAsync(new LoginCommand(usuario.Email, "Centro2026!"));

        result.IsSuccess.ShouldBeTrue();
        stored.ShouldNotBeNull();
        stored.FechaExpiracion.ShouldBeInRange(
            DateTime.UtcNow.AddDays(3).AddSeconds(-5),
            DateTime.UtcNow.AddDays(3).AddSeconds(5));
    }
}