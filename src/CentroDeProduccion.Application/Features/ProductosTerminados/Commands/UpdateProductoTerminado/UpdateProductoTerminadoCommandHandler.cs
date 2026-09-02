using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.UpdateProductoTerminado;

public class UpdateProductoTerminadoCommandHandler
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateProductoTerminadoCommand> _validator;

    public UpdateProductoTerminadoCommandHandler(
        IProductoTerminadoRepository productoTerminadoRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateProductoTerminadoCommand> validator)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateProductoTerminadoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var producto = await _productoTerminadoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (producto == null)
        {
            return Result.Failure(Error.NotFound("PRODUCTO_NOT_FOUND", "Producto terminado no encontrado"));
        }

        if (await _productoTerminadoRepository.ExistsWithSkuAsync(command.CodigoSku, command.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe otro producto con ese SKU"));
        }

        if (!producto.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(Error.Concurrency("CONCURRENCY_CONFLICT", "El producto fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        producto.Nombre = command.Nombre;
        producto.CodigoSku = command.CodigoSku;
        producto.CategoriaId = command.CategoriaId;
        producto.UnidadMedidaId = command.UnidadMedidaId;
        producto.StockMinimo = command.StockMinimo;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
