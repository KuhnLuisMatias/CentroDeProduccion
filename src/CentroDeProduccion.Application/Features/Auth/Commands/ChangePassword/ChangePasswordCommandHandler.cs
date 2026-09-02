using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ChangePasswordCommand> _validator;

    public ChangePasswordCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IValidator<ChangePasswordCommand> validator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(ChangePasswordCommand command, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario == null)
        {
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "Usuario no encontrado"));
        }

        if (!_passwordHasher.Verify(usuario.PasswordHash, command.CurrentPassword))
        {
            return Result.Failure(Error.Unauthorized("INVALID_PASSWORD", "La contraseña actual es incorrecta"));
        }

        usuario.PasswordHash = _passwordHasher.Hash(command.NewPassword);
        usuario.DebeCambiarPassword = false;

        // Revoke ALL refresh tokens (user decision: all devices)
        await _refreshTokenRepository.RevokeAllByUsuarioIdAsync(usuarioId, "Password changed", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
