using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.CreateUnidadMedida;

public class CreateUnidadMedidaCommandHandler
{
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUnidadMedidaCommand> _validator;

    public CreateUnidadMedidaCommandHandler(
        IUnidadMedidaRepository unidadMedidaRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateUnidadMedidaCommand> validator)
    {
        _unidadMedidaRepository = unidadMedidaRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateUnidadMedidaResponse>> HandleAsync(CreateUnidadMedidaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateUnidadMedidaResponse>(errors.First());
        }

        if (await _unidadMedidaRepository.ExistsWithNombreAsync(command.Nombre, null, cancellationToken))
        {
            return Result.Failure<CreateUnidadMedidaResponse>(
                Error.Conflict("UNIDAD_NOMBRE_DUPLICADO", "Ya existe una unidad de medida con ese nombre"));
        }

        if (await _unidadMedidaRepository.ExistsWithSimboloAsync(command.Simbolo, null, cancellationToken))
        {
            return Result.Failure<CreateUnidadMedidaResponse>(
                Error.Conflict("UNIDAD_SIMBOLO_DUPLICADO", "Ya existe una unidad de medida con ese símbolo"));
        }

        var unidad = new UnidadMedida
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            Simbolo = command.Simbolo,
            Tipo = command.Tipo,
            Activo = true
        };

        await _unidadMedidaRepository.AddAsync(unidad, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUnidadMedidaResponse(unidad.Id, unidad.Nombre, unidad.Simbolo, unidad.Tipo);
    }
}
