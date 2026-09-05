using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;

public class UpdateInsumoCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateInsumoCommand> _validator;

    public UpdateInsumoCommandHandler(
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateInsumoCommand> validator)
    {
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateInsumoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var insumo = await _insumoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (insumo == null)
        {
            return Result.Failure(Error.NotFound("INSUMO_NOT_FOUND", "Insumo no encontrado"));
        }

        if (await _insumoRepository.ExistsWithSkuAsync(command.CodigoSku, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe otro insumo con ese SKU"));
        }

        // Check optimistic concurrency
        if (!insumo.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(Error.Concurrency("CONCURRENCY_CONFLICT", "El registro fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        insumo.Nombre = command.Nombre;
        insumo.CodigoSku = command.CodigoSku;
        insumo.CategoriaId = command.CategoriaId;
        insumo.UnidadCompraId = command.UnidadCompraId;
        insumo.UnidadConsumoId = command.UnidadConsumoId;
        insumo.FactorConversion = command.Presentacion;
        insumo.Presentacion = command.Presentacion;
        insumo.StockMinimo = command.StockMinimo;
        insumo.ProveedorPrincipalId = command.ProveedorPrincipalId;
        insumo.Observaciones = command.Observaciones;

        if (command.PrecioUltimaCompra.HasValue)
        {
            insumo.PrecioUltimaCompra = command.PrecioUltimaCompra.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
