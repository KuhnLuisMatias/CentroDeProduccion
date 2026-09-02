using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

/// <summary>
/// Registers a return of finished products from a bar back to the production center — the
/// atomic counterpart of ConfirmRemito. The original remito must be Enviado and its bar active.
/// Every line is pre-validated against the quantity originally delivered minus everything
/// already returned for that product, before any write; then stock is incremented, one
/// DevolucionBar movement is registered per line, one negative CuentaCorrienteBar Devolucion
/// row is created and the devolucion is added, all committed by a single SaveChanges. The first
/// failing pre-check aborts with no partial writes.
/// </summary>
public class CreateDevolucionCommandHandler
{
    private readonly IDevolucionRepository _devolucionRepository;
    private readonly IRemitoRepository _remitoRepository;
    private readonly IBarRepository _barRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateDevolucionCommand> _validator;

    public CreateDevolucionCommandHandler(
        IDevolucionRepository devolucionRepository,
        IRemitoRepository remitoRepository,
        IBarRepository barRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        ICuentaCorrienteBarRepository cuentaCorrienteBarRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateDevolucionCommand> validator)
    {
        _devolucionRepository = devolucionRepository;
        _remitoRepository = remitoRepository;
        _barRepository = barRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _cuentaCorrienteBarRepository = cuentaCorrienteBarRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreateDevolucionResponse>> HandleAsync(CreateDevolucionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateDevolucionResponse>(errors.First());
        }

        var remito = await _remitoRepository.GetByIdWithLineasAsync(command.RemitoId, cancellationToken);
        if (remito == null)
        {
            return Result.Failure<CreateDevolucionResponse>(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        if (remito.Estado != EstadoRemito.Enviado)
        {
            return Result.Failure<CreateDevolucionResponse>(
                Error.Validation("REMITO_NO_ENVIADO", "Solo se pueden registrar devoluciones de remitos enviados"));
        }

        var bar = await _barRepository.GetByIdAsync(remito.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<CreateDevolucionResponse>(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<CreateDevolucionResponse>(
                Error.Validation("BAR_INACTIVO", "No se puede registrar una devolución para un bar inactivo"));
        }

        var ptIds = command.Lineas.Select(l => l.ProductoTerminadoId).Distinct().ToList();
        // Tracked load: StockActual is mutated below; AsNoTracking would silently drop it.
        var productos = await _productoTerminadoRepository.GetTrackedByIdsAsync(ptIds, cancellationToken);
        var ptDict = productos.ToDictionary(p => p.Id);

        // PHASE 1 — pre-validate ALL lines against the original remito. No writes: the first
        // failing line aborts the whole devolucion before any stock or ledger row is touched.
        var devueltoPorProducto = await _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(
            command.RemitoId, cancellationToken);
        foreach (var linea in command.Lineas)
        {
            if (!ptDict.TryGetValue(linea.ProductoTerminadoId, out var pt))
            {
                return Result.Failure<CreateDevolucionResponse>(
                    Error.NotFound("PRODUCTO_TERMINADO_NOT_FOUND", $"Producto terminado {linea.ProductoTerminadoId} no encontrado"));
            }

            var remitoLinea = remito.Lineas.FirstOrDefault(l =>
                l.TipoLinea == TipoLineaRemito.ProductoTerminado &&
                l.ProductoTerminadoId == linea.ProductoTerminadoId);
            if (remitoLinea == null)
            {
                return Result.Failure<CreateDevolucionResponse>(
                    Error.NotFound("PRODUCTO_NO_EN_REMITO", $"El producto {pt.Nombre} no existe en el remito original"));
            }

            var totalDevuelto = devueltoPorProducto.GetValueOrDefault(linea.ProductoTerminadoId);
            var disponible = remitoLinea.Cantidad - totalDevuelto;
            if (linea.Cantidad > disponible)
            {
                return Result.Failure<CreateDevolucionResponse>(
                    Error.Validation("CANTIDAD_EXCEDE_ORIGINAL",
                        $"La cantidad devuelta de {pt.Nombre} supera la cantidad original del remito: requerido {linea.Cantidad}, disponible {disponible}"));
            }
        }

        var numero = await _devolucionRepository.GetNextNumeroAsync(cancellationToken);

        var devolucion = new Devolucion
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            RemitoId = remito.Id,
            Fecha = RelojDeNegocio.Ahora,
            Observaciones = command.Observaciones,
            RecibidoPor = command.RecibidoPor,
            CreadoPor = _currentUser.UsuarioId!.Value,
            FechaCreacion = RelojDeNegocio.Ahora,
            Lineas = command.Lineas.Select(l => new DevolucionLinea
            {
                Id = Guid.NewGuid(),
                ProductoTerminadoId = l.ProductoTerminadoId,
                Cantidad = l.Cantidad,
                Lote = l.Lote
            }).ToList()
        };

        // PHASE 2 — writes. DevolucionBar movements are positive (stock goes back in) and the
        // single CuentaCorrienteBar Devolucion row is negative, mirroring ConfirmRemito's shape.
        var total = 0m;
        foreach (var linea in devolucion.Lineas)
        {
            var pt = ptDict[linea.ProductoTerminadoId];
            var remitoLinea = remito.Lineas.First(l =>
                l.TipoLinea == TipoLineaRemito.ProductoTerminado &&
                l.ProductoTerminadoId == linea.ProductoTerminadoId);

            pt.StockActual += linea.Cantidad;
            total += linea.Cantidad * remitoLinea.PrecioUnitario;

            await _movimientoStockRepository.AddAsync(new MovimientoStock
            {
                Id = Guid.NewGuid(),
                InsumoId = null,
                ProductoTerminadoId = pt.Id,
                Tipo = TipoMovimientoStock.DevolucionBar,
                Cantidad = linea.Cantidad,
                CantidadOriginal = linea.Cantidad,
                UnidadOriginalId = pt.UnidadMedidaId,
                FactorConversionAplicado = 1,
                PrecioUnitario = null,
                Motivo = $"Devolucion #{numero}",
                DocumentoOrigen = $"Devolucion #{numero}",
                UsuarioId = _currentUser.UsuarioId!.Value,
                Fecha = RelojDeNegocio.Ahora
            }, cancellationToken);
        }

        await _cuentaCorrienteBarRepository.AddAsync(new CentroDeProduccion.Domain.Entities.CuentaCorrienteBar
        {
            Id = Guid.NewGuid(),
            BarId = remito.BarId,
            TipoMovimiento = TipoMovimientoCtaCteBar.Devolucion,
            Monto = -total,
            Referencia = $"Devolucion #{numero}",
            Fecha = RelojDeNegocio.Ahora,
            RemitoId = remito.Id,
            DevolucionId = devolucion.Id,
            FechaCreacion = RelojDeNegocio.Ahora
        }, cancellationToken);

        await _devolucionRepository.AddAsync(devolucion, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreateDevolucionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La devolución fue modificada por otro usuario. Reintente."));
        }

        return new CreateDevolucionResponse(devolucion.Id, numero, remito.Id, total, devolucion.Fecha);
    }
}