using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;

public class CreateBarCommandHandler
{
    private readonly IBarRepository _barRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateBarCommand> _validator;

    public CreateBarCommandHandler(
        IBarRepository barRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateBarCommand> validator)
    {
        _barRepository = barRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateBarResponse>> HandleAsync(CreateBarCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateBarResponse>(errors.First());
        }

        if (await _barRepository.ExistsWithNombreAsync(command.Nombre, null, cancellationToken))
        {
            return Result.Failure<CreateBarResponse>(
                Error.Conflict("BAR_NOMBRE_DUPLICADO", "Ya existe un bar con este nombre"));
        }

        var bar = new Bar
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Direccion = command.Direccion,
            Encargado = command.Encargado,
            Telefono = command.Telefono,
            HorarioRecepcion = command.HorarioRecepcion,
            MargenReventaPorcentaje = command.MargenReventaPorcentaje,
            Estado = EstadoBar.Activo,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _barRepository.AddAsync(bar, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateBarResponse(bar.Id, bar.Nombre, bar.Estado, bar.RowVersion);
    }
}