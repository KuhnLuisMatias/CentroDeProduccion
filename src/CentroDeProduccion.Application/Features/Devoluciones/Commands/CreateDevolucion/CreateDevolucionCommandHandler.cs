using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

/// <summary>
/// Registers a return of finished products or insumos from a bar back to the
/// production center — the atomic counterpart of ConfirmRemito. The original remito
/// must be Enviado and its bar active. Every line is pre-validated against the
/// quantity originally delivered minus everything already returned for that item,
/// before any write. Each line carries a <see cref="DestinoDevolucion"/>: only
/// <see cref="DestinoDevolucion.ReingresoStock"/> moves stock, registers a
/// DevolucionBar movement and credits the bar's account; Cuarentena/MalEstado only
/// leave an audit movement (quantity 0) without touching stock or credit. The first
/// failing pre-check aborts with no partial writes.
/// </summary>
public class CreateDevolucionCommandHandler
{
    private readonly IDevolucionRepository _devolucionRepository;
    private readonly IRemitoRepository _remitoRepository;
    private readonly IBarRepository _barRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;
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
        IInsumoRepository insumoRepository,
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
        _insumoRepository = insumoRepository;
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

        var ptIds = command.Lineas
            .Where(l => l.ProductoTerminadoId.HasValue)
            .Select(l => l.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();
        var insumoIds = command.Lineas
            .Where(l => l.InsumoId.HasValue)
            .Select(l => l.InsumoId!.Value)
            .Distinct()
            .ToList();
        // Tracked loads: StockActual is mutated below; AsNoTracking would silently drop it.
        var productos = await _productoTerminadoRepository.GetTrackedByIdsAsync(ptIds, cancellationToken);
        var insumos = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var ptDict = productos.ToDictionary(p => p.Id);
        var insumoDict = insumos.ToDictionary(i => i.Id);

        // PHASE 1 — pre-validate ALL lines against the original remito. No writes: the first
        // failing line aborts the whole devolucion before any stock or ledger row is touched.
        var devueltoPorProducto = await _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(
            command.RemitoId, cancellationToken);
        var devueltoPorInsumo = await _devolucionRepository.GetTotalesDevueltosInsumosPorRemitoAsync(
            command.RemitoId, cancellationToken);
        foreach (var linea in command.Lineas)
        {
            var resuelta = ResolverLinea(
                linea, remito, ptDict, insumoDict, devueltoPorProducto, devueltoPorInsumo);
            if (resuelta.IsFailure)
            {
                return Result.Failure<CreateDevolucionResponse>(resuelta.Error);
            }
            var (nombre, disponible, _) = resuelta.Value;
            if (linea.Cantidad > disponible)
            {
                return Result.Failure<CreateDevolucionResponse>(
                    Error.Validation("CANTIDAD_EXCEDE_ORIGINAL",
                        $"La cantidad devuelta de {nombre} supera la cantidad original del remito: requerido {linea.Cantidad}, disponible {disponible}"));
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
                InsumoId = l.InsumoId,
                Cantidad = l.Cantidad,
                Lote = l.Lote,
                Destino = l.Destino
            }).ToList()
        };

        // PHASE 2 — writes. ReingresoStock lines behave as before (stock in, DevolucionBar
        // movement, negative cta cte row); Cuarentena/MalEstado only leave an audit movement
        // (quantity 0) without touching stock or credit.
        var total = 0m;
        foreach (var linea in devolucion.Lineas)
        {
            var esPt = linea.ProductoTerminadoId.HasValue;
            var nombre = esPt
                ? ptDict[linea.ProductoTerminadoId!.Value].Nombre
                : insumoDict[linea.InsumoId!.Value].Nombre;
            var remitoLinea = remito.Lineas.First(l =>
                esPt
                    ? l.TipoLinea == TipoLineaRemito.ProductoTerminado && l.ProductoTerminadoId == linea.ProductoTerminadoId
                    : l.TipoLinea == TipoLineaRemito.Insumo && l.InsumoId == linea.InsumoId);

            if (linea.Destino == DestinoDevolucion.ReingresoStock)
            {
                if (esPt)
                {
                    ptDict[linea.ProductoTerminadoId!.Value].StockActual += linea.Cantidad;
                }
                else
                {
                    insumoDict[linea.InsumoId!.Value].StockActual += linea.Cantidad;
                }
                total += linea.Cantidad * remitoLinea.PrecioUnitario;

                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = linea.InsumoId,
                    ProductoTerminadoId = linea.ProductoTerminadoId,
                    Tipo = TipoMovimientoStock.DevolucionBar,
                    Cantidad = linea.Cantidad,
                    CantidadOriginal = linea.Cantidad,
                    UnidadOriginalId = esPt
                        ? ptDict[linea.ProductoTerminadoId!.Value].UnidadMedidaId
                        : insumoDict[linea.InsumoId!.Value].UnidadConsumoId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = null,
                    Motivo = $"Devolucion #{numero}",
                    DocumentoOrigen = $"Devolucion #{numero}",
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }
            else
            {
                var tipoAuditoria = linea.Destino == DestinoDevolucion.Cuarentena
                    ? TipoMovimientoStock.DevolucionCuarentena
                    : TipoMovimientoStock.DevolucionMalEstado;
                var destinoTexto = linea.Destino == DestinoDevolucion.Cuarentena ? "Cuarentena" : "mal estado";
                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = linea.InsumoId,
                    ProductoTerminadoId = linea.ProductoTerminadoId,
                    Tipo = tipoAuditoria,
                    Cantidad = 0,
                    CantidadOriginal = linea.Cantidad,
                    UnidadOriginalId = esPt
                        ? ptDict[linea.ProductoTerminadoId!.Value].UnidadMedidaId
                        : insumoDict[linea.InsumoId!.Value].UnidadConsumoId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = null,
                    Motivo = $"Devolucion #{numero} ({destinoTexto}: {nombre})",
                    DocumentoOrigen = $"Devolucion #{numero}",
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }
        }

        // Solo las líneas con reingreso generan crédito: sin total no hay fila.
        if (total != 0)
        {
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
        }

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

    /// <summary>Resolves (display name, returnable quantity, original unit price) for a line,
    /// or a failure when the item is missing or not part of the original remito.</summary>
    private static Result<(string Nombre, decimal Disponible, decimal Precio)> ResolverLinea(
        CreateDevolucionLineaCommand linea,
        Remito remito,
        Dictionary<Guid, ProductoTerminado> ptDict,
        Dictionary<Guid, Insumo> insumoDict,
        Dictionary<Guid, decimal> devueltoPorProducto,
        Dictionary<Guid, decimal> devueltoPorInsumo)
    {
        if (linea.ProductoTerminadoId.HasValue)
        {
            if (!ptDict.TryGetValue(linea.ProductoTerminadoId.Value, out var pt))
            {
                return Result.Failure<(string, decimal, decimal)>(Error.NotFound(
                    "PRODUCTO_TERMINADO_NOT_FOUND", $"Producto terminado {linea.ProductoTerminadoId} no encontrado"));
            }
            var remitoLinea = remito.Lineas.FirstOrDefault(l =>
                l.TipoLinea == TipoLineaRemito.ProductoTerminado &&
                l.ProductoTerminadoId == linea.ProductoTerminadoId);
            if (remitoLinea == null)
            {
                return Result.Failure<(string, decimal, decimal)>(Error.NotFound(
                    "PRODUCTO_NO_EN_REMITO", $"El producto {pt.Nombre} no existe en el remito original"));
            }
            var disponible = remitoLinea.Cantidad - devueltoPorProducto.GetValueOrDefault(linea.ProductoTerminadoId.Value);
            return (pt.Nombre, disponible, remitoLinea.PrecioUnitario);
        }

        if (!linea.InsumoId.HasValue || !insumoDict.TryGetValue(linea.InsumoId.Value, out var insumo))
        {
            return Result.Failure<(string, decimal, decimal)>(Error.NotFound(
                "INSUMO_NOT_FOUND", $"Insumo {linea.InsumoId} no encontrado"));
        }
        var remitoInsumo = remito.Lineas.FirstOrDefault(l =>
            l.TipoLinea == TipoLineaRemito.Insumo && l.InsumoId == linea.InsumoId);
        if (remitoInsumo == null)
        {
            return Result.Failure<(string, decimal, decimal)>(Error.NotFound(
                "INSUMO_NO_EN_REMITO", $"El insumo {insumo.Nombre} no existe en el remito original"));
        }
        var disponibleInsumo = remitoInsumo.Cantidad - devueltoPorInsumo.GetValueOrDefault(linea.InsumoId.Value);
        return (insumo.Nombre, disponibleInsumo, remitoInsumo.PrecioUnitario);
    }
}
