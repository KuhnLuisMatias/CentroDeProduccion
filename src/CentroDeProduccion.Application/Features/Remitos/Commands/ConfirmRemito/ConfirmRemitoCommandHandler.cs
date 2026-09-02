using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;

/// <summary>
/// Confirms (sends) a remito — the critical atomic operation that moves stock out of the
/// production center and records the bar's debt. Every line is pre-validated for stock
/// availability before any write, then stock is decremented, one VentaBar/Reventa movement is
/// registered per line, one CuentaCorrienteBar Remito row is created and the remito transitions
/// to Enviado, all committed by a single SaveChanges. The first failing pre-check aborts with
/// no partial writes.
/// </summary>
public class ConfirmRemitoCommandHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IBarRepository _barRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ConfirmRemitoCommand> _validator;

    public ConfirmRemitoCommandHandler(
        IRemitoRepository remitoRepository,
        IBarRepository barRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IInsumoRepository insumoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        ICuentaCorrienteBarRepository cuentaCorrienteBarRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<ConfirmRemitoCommand> validator)
    {
        _remitoRepository = remitoRepository;
        _barRepository = barRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _insumoRepository = insumoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _cuentaCorrienteBarRepository = cuentaCorrienteBarRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<ConfirmRemitoResponse>> HandleAsync(ConfirmRemitoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<ConfirmRemitoResponse>(errors.First());
        }

        var remito = await _remitoRepository.GetByIdWithLineasAsync(command.RemitoId, cancellationToken);
        if (remito == null)
        {
            return Result.Failure<ConfirmRemitoResponse>(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        if (!remito.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure<ConfirmRemitoResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        if (remito.Estado is not (EstadoRemito.Pendiente or EstadoRemito.EnProceso))
        {
            return Result.Failure<ConfirmRemitoResponse>(
                Error.Validation("REMITO_NO_CONFIRMABLE", "Solo se pueden confirmar remitos en estado Pendiente o EnProceso"));
        }

        var bar = await _barRepository.GetByIdAsync(remito.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<ConfirmRemitoResponse>(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<ConfirmRemitoResponse>(
                Error.Validation("BAR_INACTIVO", "No se puede confirmar un remito para un bar inactivo"));
        }

        if (remito.Lineas.Count == 0)
        {
            return Result.Failure<ConfirmRemitoResponse>(
                Error.Validation("REMITO_SIN_LINEAS", "El remito debe tener al menos una línea para confirmar"));
        }

        var ptIds = remito.Lineas
            .Where(l => l.TipoLinea == TipoLineaRemito.ProductoTerminado && l.ProductoTerminadoId.HasValue)
            .Select(l => l.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();

        var insumoIds = remito.Lineas
            .Where(l => l.TipoLinea == TipoLineaRemito.Insumo && l.InsumoId.HasValue)
            .Select(l => l.InsumoId!.Value)
            .Distinct()
            .ToList();

        // Tracked load: StockActual is mutated below; AsNoTracking would silently drop it.
        var productos = await _productoTerminadoRepository.GetTrackedByIdsAsync(ptIds, cancellationToken);
        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);

        var ptDict = productos.ToDictionary(p => p.Id);
        var insumoDict = insumos.ToDictionary(i => i.Id);

        // PHASE 1 — stock pre-check. No writes: the first failing line aborts the whole
        // confirmation before any stock is touched, so a partial write is impossible.
        foreach (var linea in remito.Lineas)
        {
            if (linea.TipoLinea == TipoLineaRemito.ProductoTerminado)
            {
                var ptId = linea.ProductoTerminadoId ?? Guid.Empty;
                if (!ptDict.TryGetValue(ptId, out var pt))
                {
                    return Result.Failure<ConfirmRemitoResponse>(
                        Error.NotFound("PRODUCTO_TERMINADO_NOT_FOUND", $"Producto terminado {ptId} no encontrado"));
                }

                if (pt.StockActual < linea.Cantidad)
                {
                    return Result.Failure<ConfirmRemitoResponse>(
                        Error.Validation("STOCK_INSUFICIENTE",
                            $"Stock insuficiente para {pt.Nombre}: requerido {linea.Cantidad}, disponible {pt.StockActual}"));
                }

                // Block sale of expired products (spec §5.5, mirrors RegisterMovementCommandHandler)
                if (pt.FechaVencimiento < RelojDeNegocio.Ahora)
                {
                    return Result.Failure<ConfirmRemitoResponse>(
                        Error.Validation("PRODUCTO_VENCIDO", $"El producto {pt.Nombre} está vencido y no puede venderse"));
                }
            }
            else
            {
                var insumoId = linea.InsumoId ?? Guid.Empty;
                if (!insumoDict.TryGetValue(insumoId, out var insumo))
                {
                    return Result.Failure<ConfirmRemitoResponse>(
                        Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {insumoId} no encontrado"));
                }

                if (insumo.StockActual < linea.Cantidad)
                {
                    return Result.Failure<ConfirmRemitoResponse>(
                        Error.Validation("STOCK_INSUFICIENTE_INSUMO",
                            $"Stock insuficiente para {insumo.Nombre}: requerido {linea.Cantidad}, disponible {insumo.StockActual} (en unidad de consumo)"));
                }
            }
        }

        var total = remito.Lineas.Sum(l => l.Subtotal);

        // PHASE 2 — writes. The stock logic is inlined from RegisterMovementCommandHandler
        // (which commits per call and would break atomicity): each row mirrors its MovimientoStock
        // shape with signed quantity, and the line quantities are already in the consumption unit
        // (remito lines are compared against StockActual directly), so no unit conversion applies.
        foreach (var linea in remito.Lineas)
        {
            if (linea.TipoLinea == TipoLineaRemito.ProductoTerminado)
            {
                var pt = ptDict[linea.ProductoTerminadoId!.Value];
                pt.StockActual -= linea.Cantidad;

                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = null,
                    ProductoTerminadoId = pt.Id,
                    Tipo = TipoMovimientoStock.VentaBar,
                    Cantidad = -linea.Cantidad,
                    CantidadOriginal = linea.Cantidad,
                    UnidadOriginalId = pt.UnidadMedidaId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = null,
                    Motivo = $"Remito #{remito.NumeroRemito}",
                    DocumentoOrigen = remito.Id.ToString(),
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }
            else
            {
                var insumo = insumoDict[linea.InsumoId!.Value];
                insumo.StockActual -= linea.Cantidad;

                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = insumo.Id,
                    ProductoTerminadoId = null,
                    Tipo = TipoMovimientoStock.Reventa,
                    Cantidad = -linea.Cantidad,
                    CantidadOriginal = linea.Cantidad,
                    UnidadOriginalId = insumo.UnidadConsumoId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = null,
                    Motivo = $"Remito #{remito.NumeroRemito}",
                    DocumentoOrigen = remito.Id.ToString(),
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }
        }

        await _cuentaCorrienteBarRepository.AddAsync(new CentroDeProduccion.Domain.Entities.CuentaCorrienteBar
        {
            Id = Guid.NewGuid(),
            BarId = remito.BarId,
            TipoMovimiento = TipoMovimientoCtaCteBar.Remito,
            Monto = total,
            Referencia = $"Remito #{remito.NumeroRemito}",
            Fecha = RelojDeNegocio.Ahora,
            RemitoId = remito.Id,
            FechaCreacion = RelojDeNegocio.Ahora
        }, cancellationToken);

        remito.Estado = EstadoRemito.Enviado;
        remito.FechaEnvio = RelojDeNegocio.Ahora;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<ConfirmRemitoResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Reintente."));
        }

        return new ConfirmRemitoResponse(remito.Id, remito.NumeroRemito, remito.Estado, total, remito.FechaEnvio);
    }
}