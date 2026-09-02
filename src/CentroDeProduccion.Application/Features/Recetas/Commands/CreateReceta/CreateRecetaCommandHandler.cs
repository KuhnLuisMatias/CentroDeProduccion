using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandHandler
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRecetaCommand> _validator;

    public CreateRecetaCommandHandler(
        IRecetaRepository recetaRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateRecetaCommand> validator)
    {
        _recetaRepository = recetaRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateRecetaResponse>> HandleAsync(CreateRecetaCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateRecetaResponse>(errors.First());
        }

        if (await _recetaRepository.ExistsWithSkuAsync(command.CodigoSku, null, cancellationToken))
        {
            return Result.Failure<CreateRecetaResponse>(
                Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe una receta con ese SKU"));
        }

        var receta = new Receta
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            CodigoSku = command.CodigoSku,
            CategoriaId = command.CategoriaId,
            UnidadMedidaId = command.UnidadMedidaId,
            Descripcion = command.Descripcion,
            Estado = EstadoReceta.Activa,
            Activo = true,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        foreach (var detalle in command.Insumos)
        {
            receta.Insumos.Add(new RecetaInsumo
            {
                Id = Guid.NewGuid(),
                RecetaId = receta.Id,
                InsumoId = detalle.InsumoId,
                RecetaOrigenId = detalle.RecetaOrigenId,
                CantidadNecesaria = detalle.CantidadNecesaria,
                UnidadMedidaId = detalle.UnidadMedidaId,
                Observaciones = detalle.Observaciones
            });
        }

        await _recetaRepository.AddAsync(receta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateRecetaResponse(receta.Id, receta.Nombre, receta.CodigoSku);
    }
}
