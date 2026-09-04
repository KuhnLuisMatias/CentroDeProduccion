using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Bares.Commands.UpdateBar;

public class UpdateBarCommandHandler
{
    private readonly IBarRepository _barRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateBarCommand> _validator;

    public UpdateBarCommandHandler(
        IBarRepository barRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateBarCommand> validator)
    {
        _barRepository = barRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateBarCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var bar = await _barRepository.GetByIdAsync(command.Id, cancellationToken);
        if (bar == null)
        {
            return Result.Failure(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (!bar.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El bar fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        if (await _barRepository.ExistsWithNombreAsync(command.Nombre, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("BAR_NOMBRE_DUPLICADO", "Ya existe otro bar con ese nombre"));
        }

        bar.Nombre = command.Nombre;
        bar.Direccion = command.Direccion;
        bar.Encargado = command.Encargado;
        bar.Telefono = command.Telefono;
        bar.HorarioRecepcion = command.HorarioRecepcion;
        bar.MargenReventaPorcentaje = command.MargenReventaPorcentaje;
        bar.Estado = command.Estado;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El bar fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        return Result.Success();
    }
}