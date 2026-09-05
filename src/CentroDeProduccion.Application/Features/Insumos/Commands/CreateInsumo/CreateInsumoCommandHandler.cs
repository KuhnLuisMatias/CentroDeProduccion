using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Insumos.Commands.CreateInsumo;

public class CreateInsumoCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateInsumoCommand> _validator;

    public CreateInsumoCommandHandler(
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateInsumoCommand> validator)
    {
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<CreateInsumoResponse>> HandleAsync(CreateInsumoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateInsumoResponse>(errors.First());
        }

        if (await _insumoRepository.ExistsWithSkuAsync(command.CodigoSku, null, cancellationToken))
        {
            return Result.Failure<CreateInsumoResponse>(
                Error.Conflict("SKU_ALREADY_EXISTS", "Ya existe un insumo con ese SKU"));
        }

        var insumo = new Insumo
        {
            Id = Guid.NewGuid(),
            Nombre = command.Nombre,
            CodigoSku = command.CodigoSku,
            CategoriaId = command.CategoriaId,
            UnidadCompraId = command.UnidadCompraId,
            UnidadConsumoId = command.UnidadConsumoId,
            FactorConversion = command.Presentacion,
            Presentacion = command.Presentacion,
            StockMinimo = command.StockMinimo,
            StockActual = 0,
            PrecioUltimaCompra = command.PrecioUltimaCompra ?? 0,
            ProveedorPrincipalId = command.ProveedorPrincipalId,
            Observaciones = command.Observaciones,
            Activo = true,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _insumoRepository.AddAsync(insumo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateInsumoResponse(
            insumo.Id,
            insumo.Nombre,
            insumo.CodigoSku,
            insumo.CategoriaId,
            "", // Categoria nombre would need to be loaded
            insumo.UnidadCompraId,
            "", // Unidad compra simbolo
            insumo.UnidadConsumoId,
            "", // Unidad consumo simbolo
            insumo.FactorConversion,
            insumo.Presentacion,
            insumo.StockMinimo,
            insumo.StockActual,
            insumo.PrecioUltimaCompra,
            insumo.ProveedorPrincipalId,
            insumo.Observaciones);
    }
}
