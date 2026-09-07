using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;

public class UpdateInsumoCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<UpdateInsumoCommand> _validator;

    public UpdateInsumoCommandHandler(
        IInsumoRepository insumoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<UpdateInsumoCommand> validator)
    {
        _insumoRepository = insumoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
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

        // Ajuste de stock con historial: registra un movimiento por la diferencia.
        // El stock resultante nunca puede quedar en negativo.
        if (command.StockActual.HasValue && command.StockActual.Value != insumo.StockActual)
        {
            var diferencia = command.StockActual.Value - insumo.StockActual;
            if (insumo.StockActual + diferencia < 0)
            {
                return Result.Failure(Error.Validation(
                    "INSUFFICIENT_STOCK",
                    $"Stock insuficiente de {insumo.Nombre}. Disponible: {insumo.StockActual}"));
            }

            insumo.StockActual += diferencia;

            await _movimientoStockRepository.AddAsync(new MovimientoStock
            {
                Id = Guid.NewGuid(),
                InsumoId = insumo.Id,
                Tipo = diferencia > 0 ? TipoMovimientoStock.AjustePositivo : TipoMovimientoStock.AjusteNegativo,
                Cantidad = diferencia,
                CantidadOriginal = Math.Abs(diferencia),
                UnidadOriginalId = insumo.UnidadConsumoId,
                FactorConversionAplicado = insumo.FactorConversion,
                Motivo = "Ajuste desde edición de insumo",
                DocumentoOrigen = insumo.Id.ToString(),
                UsuarioId = _currentUser.UsuarioId!.Value,
                Fecha = RelojDeNegocio.Ahora
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
