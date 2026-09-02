using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandHandler
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateEmpleadoCommand> _validator;

    public UpdateEmpleadoCommandHandler(
        IEmpleadoRepository empleadoRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateEmpleadoCommand> validator)
    {
        _empleadoRepository = empleadoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateEmpleadoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var empleado = await _empleadoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (empleado == null)
        {
            return Result.Failure(Error.NotFound("EMPLEADO_NOT_FOUND", "Empleado no encontrado"));
        }

        // DNI is immutable (spec "Empleado Modification: DNI immutable") — reject any change.
        if (command.Dni != empleado.Dni)
        {
            return Result.Failure(Error.Validation("DNI_INMUTABLE", "El DNI no puede modificarse"));
        }

        if (await _empleadoRepository.ExistsWithDniAsync(command.Dni, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("DNI_ALREADY_EXISTS", "Ya existe otro empleado con ese DNI"));
        }

        if (!empleado.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(Error.Concurrency("CONCURRENCY_CONFLICT", "El empleado fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        empleado.Nombre = command.Nombre;
        empleado.Apellido = command.Apellido;
        empleado.Cargo = command.Cargo;
        empleado.TarifaPorHora = command.TarifaPorHora;
        empleado.Categoria = command.Categoria;
        empleado.Activo = command.Activo;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
