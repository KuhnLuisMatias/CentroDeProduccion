using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;

/// <summary>
/// Registers a stock movement against either an insumo or a finished product, updating the
/// target's stock atomically within the same UnitOfWork transaction. Insumo movements convert
/// the entered quantity to the consumption unit and update the last purchase price on buys;
/// finished-product movements block expired sales.
/// </summary>
public class RegisterMovementCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RegisterMovementCommand> _validator;

    public RegisterMovementCommandHandler(
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<RegisterMovementCommand> validator)
    {
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<RegisterMovementResponse>> HandleAsync(RegisterMovementCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegisterMovementResponse>(errors.First());
        }

        return command.InsumoId.HasValue
            ? await RegistrarInsumoAsync(command, cancellationToken)
            : await RegistrarProductoAsync(command, cancellationToken);
    }

    private async Task<Result<RegisterMovementResponse>> RegistrarInsumoAsync(RegisterMovementCommand command, CancellationToken cancellationToken)
    {
        var insumo = await _insumoRepository.GetByIdAsync(command.InsumoId!.Value, cancellationToken);
        if (insumo == null)
        {
            return Result.Failure<RegisterMovementResponse>(Error.NotFound("INSUMO_NOT_FOUND", "Insumo no encontrado"));
        }

        var cantidadConsumo = ConversionUnidades.ToUnidadConsumo(
            command.Cantidad, command.UnidadOriginalId, insumo.UnidadCompraId, insumo.UnidadConsumoId, insumo.FactorConversion);

        var signedQuantity = command.Tipo switch
        {
            TipoMovimientoStock.Compra => cantidadConsumo,
            TipoMovimientoStock.AjustePositivo => cantidadConsumo,
            TipoMovimientoStock.AjusteNegativo => -cantidadConsumo,
            TipoMovimientoStock.DevolucionProveedor => -cantidadConsumo,
            TipoMovimientoStock.ConsumoProduccion => -cantidadConsumo,
            TipoMovimientoStock.Reventa => -cantidadConsumo,
            _ => throw new ArgumentException("Tipo de movimiento de insumo no soportado")
        };

        if (signedQuantity < 0 && insumo.StockActual + signedQuantity < 0)
        {
            return Result.Failure<RegisterMovementResponse>(
                Error.Validation("INSUFFICIENT_STOCK", $"Stock insuficiente de {insumo.Nombre}. Disponible: {insumo.StockActual}"));
        }

        var stockAnterior = insumo.StockActual;
        insumo.StockActual += signedQuantity;

        if (command.Tipo == TipoMovimientoStock.Compra && command.PrecioUnitario.HasValue)
        {
            insumo.PrecioUltimaCompra = command.PrecioUnitario.Value;
        }

        var movimiento = new MovimientoStock
        {
            Id = Guid.NewGuid(),
            InsumoId = insumo.Id,
            Tipo = command.Tipo,
            Cantidad = signedQuantity,
            CantidadOriginal = command.Cantidad,
            UnidadOriginalId = command.UnidadOriginalId,
            FactorConversionAplicado = insumo.FactorConversion,
            PrecioUnitario = command.PrecioUnitario,
            Motivo = command.Motivo,
            DocumentoOrigen = command.DocumentoOrigen,
            UsuarioId = _currentUser.UsuarioId!.Value,
            Fecha = RelojDeNegocio.Ahora
        };

        await _movimientoStockRepository.AddAsync(movimiento, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<RegisterMovementResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El insumo fue modificado por otro movimiento. Reintente."));
        }

        return new RegisterMovementResponse(movimiento.Id, insumo.Id, insumo.Nombre, stockAnterior, signedQuantity, insumo.StockActual, movimiento.Fecha);
    }

    private async Task<Result<RegisterMovementResponse>> RegistrarProductoAsync(RegisterMovementCommand command, CancellationToken cancellationToken)
    {
        var producto = await _productoTerminadoRepository.GetByIdAsync(command.ProductoTerminadoId!.Value, cancellationToken);
        if (producto == null)
        {
            return Result.Failure<RegisterMovementResponse>(Error.NotFound("PRODUCTO_NOT_FOUND", "Producto terminado no encontrado"));
        }

        // Block sale of expired products (spec §5.5)
        if (command.Tipo == TipoMovimientoStock.VentaBar && producto.FechaVencimiento < RelojDeNegocio.Ahora)
        {
            return Result.Failure<RegisterMovementResponse>(
                Error.Validation("PRODUCTO_VENCIDO", $"El producto {producto.Nombre} está vencido y no puede venderse"));
        }

        var signedQuantity = command.Tipo switch
        {
            TipoMovimientoStock.Produccion => command.Cantidad,
            TipoMovimientoStock.DevolucionBar => command.Cantidad,
            TipoMovimientoStock.AjustePositivo => command.Cantidad,
            TipoMovimientoStock.VentaBar => -command.Cantidad,
            TipoMovimientoStock.BajaPorVencimiento => -command.Cantidad,
            TipoMovimientoStock.AjusteNegativo => -command.Cantidad,
            _ => throw new ArgumentException("Tipo de movimiento de producto no soportado")
        };

        if (signedQuantity < 0 && producto.StockActual + signedQuantity < 0)
        {
            return Result.Failure<RegisterMovementResponse>(
                Error.Validation("INSUFFICIENT_STOCK", $"Stock insuficiente de {producto.Nombre}. Disponible: {producto.StockActual}"));
        }

        var stockAnterior = producto.StockActual;
        producto.StockActual += signedQuantity;

        if (command.Tipo == TipoMovimientoStock.BajaPorVencimiento)
        {
            producto.Estado = EstadoProductoTerminado.Vencido;
        }

        var movimiento = new MovimientoStock
        {
            Id = Guid.NewGuid(),
            ProductoTerminadoId = producto.Id,
            Tipo = command.Tipo,
            Cantidad = signedQuantity,
            CantidadOriginal = command.Cantidad,
            UnidadOriginalId = command.UnidadOriginalId,
            FactorConversionAplicado = 1,
            PrecioUnitario = null,
            Motivo = command.Motivo,
            DocumentoOrigen = command.DocumentoOrigen,
            UsuarioId = _currentUser.UsuarioId!.Value,
            Fecha = RelojDeNegocio.Ahora
        };

        await _movimientoStockRepository.AddAsync(movimiento, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<RegisterMovementResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El producto fue modificado por otro movimiento. Reintente."));
        }

        return new RegisterMovementResponse(movimiento.Id, producto.Id, producto.Nombre, stockAnterior, signedQuantity, producto.StockActual, movimiento.Fecha);
    }
}
