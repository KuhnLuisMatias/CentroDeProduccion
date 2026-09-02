using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;

public class CreateProductoTerminadoCommandHandler
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductoTerminadoCommand> _validator;

    public CreateProductoTerminadoCommandHandler(
        IProductoTerminadoRepository productoTerminadoRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateProductoTerminadoCommand> validator)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateProductoTerminadoResponse>> HandleAsync(CreateProductoTerminadoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateProductoTerminadoResponse>(errors.First());
        }

        if (await _productoTerminadoRepository.ExistsWithSkuAsync(command.CodigoSku, null, cancellationToken))
        {
            return Result.Failure<CreateProductoTerminadoResponse>(
                Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe un producto terminado con ese SKU"));
        }

        var producto = new ProductoTerminado
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            CodigoSku = command.CodigoSku,
            CategoriaId = command.CategoriaId,
            UnidadMedidaId = command.UnidadMedidaId,
            StockActual = 0,
            FechaProduccion = RelojDeNegocio.Ahora,
            FechaVencimiento = RelojDeNegocio.Ahora.AddDays(30),
            Lote = string.Empty,
            Estado = EstadoProductoTerminado.Disponible,
            Activo = true,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _productoTerminadoRepository.AddAsync(producto, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProductoTerminadoResponse(producto.Id, producto.Nombre, producto.CodigoSku);
    }
}
