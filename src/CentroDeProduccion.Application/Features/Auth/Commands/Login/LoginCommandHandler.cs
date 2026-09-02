using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace CentroDeProduccion.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler
{
    private const double DefaultRefreshTokenExpirationDays = 7;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LoginCommand> _validator;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITokenHasher tokenHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IValidator<LoginCommand> validator,
        IConfiguration configuration)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tokenHasher = tokenHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _configuration = configuration;
    }

    private double RefreshTokenExpirationDays()
    {
        var value = _configuration.GetSection("Jwt")["RefreshTokenExpirationDays"];
        return double.TryParse(value, out var days) ? days : DefaultRefreshTokenExpirationDays;
    }

    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<LoginResponse>(errors.First());
        }

        var usuario = await _usuarioRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (usuario == null || !_passwordHasher.Verify(usuario.PasswordHash, command.Password))
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized("INVALID_CREDENTIALS", "Email o contraseña incorrectos"));
        }

        if (!usuario.Activo)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized("USER_INACTIVE", "La cuenta está desactivada"));
        }

        var token = _jwtTokenService.GenerateAccessToken(usuario);

        var tokenCrudo = Guid.NewGuid().ToString();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(tokenCrudo),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays()),
            FechaCreacion = DateTime.UtcNow,
            Revocado = false,
            FamiliaId = Guid.NewGuid()
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            usuario.Id,
            usuario.Email,
            usuario.Nombre,
            usuario.Apellido,
            usuario.Rol.ToString(),
            usuario.DebeCambiarPassword,
            token,
            tokenCrudo);
    }
}
