using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.UpdateUnidadMedida;

public class UpdateUnidadMedidaCommandHandler
{
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateUnidadMedidaCommand> _validator;

    public UpdateUnidadMedidaCommandHandler(
        IUnidadMedidaRepository unidadMedidaRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateUnidadMedidaCommand> validator)
    {
        _unidadMedidaRepository = unidadMedidaRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateUnidadMedidaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var unidad = await _unidadMedidaRepository.GetByIdAsync(command.Id, cancellationToken);
        if (unidad == null)
        {
            return Result.Failure(Error.NotFound("UNIDAD_NOT_FOUND", "Unidad de medida no encontrada"));
        }

        if (await _unidadMedidaRepository.ExistsWithNombreAsync(command.Nombre, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("UNIDAD_NOMBRE_DUPLICADO", "Ya existe otra unidad de medida con ese nombre"));
        }

        if (await _unidadMedidaRepository.ExistsWithSimboloAsync(command.Simbolo, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("UNIDAD_SIMBOLO_DUPLICADO", "Ya existe otra unidad de medida con ese símbolo"));
        }

        unidad.Nombre = command.Nombre;
        unidad.Simbolo = command.Simbolo;
        unidad.Tipo = command.Tipo;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
