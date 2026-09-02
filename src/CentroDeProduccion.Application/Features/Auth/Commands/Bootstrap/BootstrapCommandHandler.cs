using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Auth.Commands.Bootstrap;

/// <summary>
/// Creates the first administrator when the system has no users yet (design D3 bootstrap).
/// Succeeds only while <see cref="IUsuarioRepository.AnyAsync"/> is false; once any user
/// exists, further account creation is an admin-only operation via the register endpoint.
/// </summary>
public class BootstrapCommandHandler
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<BootstrapCommand> _validator;
    private readonly IUnitOfWork _unitOfWork;

    public BootstrapCommandHandler(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IValidator<BootstrapCommand> validator,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BootstrapResponse>> HandleAsync(BootstrapCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<BootstrapResponse>(errors.First());
        }

        if (await _usuarioRepository.AnyAsync(cancellationToken))
        {
            return Result.Failure<BootstrapResponse>(
                Error.Conflict("ALREADY_BOOTSTRAPPED", "Ya existe al menos un usuario. Use el endpoint de registro autenticado."));
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Apellido = command.Apellido,
            Email = command.Email,
            PasswordHash = _passwordHasher.Hash(command.Password),
            Rol = Rol.Administrador,
            Telefono = null,
            Direccion = null,
            DebeCambiarPassword = true,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        await _usuarioRepository.AddAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BootstrapResponse(usuario.Id, usuario.Email, usuario.Nombre, usuario.Apellido);
    }
}
