using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterCommand> _validator;

    public RegisterCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITokenHasher tokenHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegisterCommand> validator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tokenHasher = tokenHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegisterResponse>(errors.First());
        }

        var existingUser = await _usuarioRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser != null)
        {
            return Result.Failure<RegisterResponse>(
                Error.Conflict("USER_ALREADY_EXISTS", "Ya existe un usuario con ese email"));
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Apellido = command.Apellido,
            Email = command.Email,
            PasswordHash = _passwordHasher.Hash(command.Password),
            Rol = command.Rol,
            Telefono = command.Telefono,
            Direccion = command.Direccion,
            DebeCambiarPassword = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        await _usuarioRepository.AddAsync(usuario, cancellationToken);

        var token = _jwtTokenService.GenerateAccessToken(usuario);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _tokenHasher.Hash(Guid.NewGuid().ToString()),
            UsuarioId = usuario.Id,
            FechaExpiracion = DateTime.UtcNow.AddDays(7),
            FechaCreacion = DateTime.UtcNow,
            Revocado = false,
            FamiliaId = Guid.NewGuid()
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            usuario.Id,
            usuario.Email,
            usuario.Nombre,
            usuario.Apellido,
            token,
            refreshToken.TokenHash);
    }
}
