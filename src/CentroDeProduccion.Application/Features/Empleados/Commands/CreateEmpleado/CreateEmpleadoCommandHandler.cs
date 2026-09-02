using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandHandler
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateEmpleadoCommand> _validator;

    public CreateEmpleadoCommandHandler(
        IEmpleadoRepository empleadoRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateEmpleadoCommand> validator)
    {
        _empleadoRepository = empleadoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateEmpleadoResponse>> HandleAsync(CreateEmpleadoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateEmpleadoResponse>(errors.First());
        }

        if (await _empleadoRepository.ExistsWithDniAsync(command.Dni, null, cancellationToken))
        {
            return Result.Failure<CreateEmpleadoResponse>(
                Error.Conflict("DNI_ALREADY_EXISTS", "Ya existe un empleado con ese DNI"));
        }

        var empleado = new Empleado
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Apellido = command.Apellido,
            Dni = command.Dni,
            Cargo = command.Cargo,
            TarifaPorHora = command.TarifaPorHora,
            Categoria = command.Categoria,
            Activo = true,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _empleadoRepository.AddAsync(empleado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateEmpleadoResponse(empleado.Id, empleado.Nombre, empleado.Apellido, empleado.Dni, empleado.TarifaPorHora);
    }
}
