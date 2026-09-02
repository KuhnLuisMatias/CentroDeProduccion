using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;

/// <summary>
/// Closes a guided inventory session — the critical atomic operation that reconciles stock
/// against the counted quantities. Every conteo with a difference is pre-validated (counts
/// cannot be negative) before any write, then for each difference the target's StockActual is
/// set to the counted quantity and one AjustePositivo/AjusteNegativo movement is registered.
/// The stock logic is inlined from RegisterMovementCommandHandler (which commits per call and
/// would break atomicity): the whole close, all adjustments and the session's Cerrada state
/// transition, commits through a single SaveChanges. The first failing pre-check aborts with
/// no partial writes.
/// </summary>
public class ConfirmInventarioSesionCommandHandler
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ConfirmInventarioSesionCommand> _validator;

    public ConfirmInventarioSesionCommandHandler(
        IInventarioSesionRepository inventarioSesionRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        ProductoTerminadoCostoResolver costoResolver,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<ConfirmInventarioSesionCommand> validator)
    {
        _inventarioSesionRepository = inventarioSesionRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _costoResolver = costoResolver;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<ConfirmInventarioSesionResponse>> HandleAsync(
        ConfirmInventarioSesionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<ConfirmInventarioSesionResponse>(errors.First());
        }

        var session = await _inventarioSesionRepository.GetByIdWithConteosAsync(command.InventarioSesionId, cancellationToken);
        if (session == null)
        {
            return Result.Failure<ConfirmInventarioSesionResponse>(
                Error.NotFound("SESION_NOT_FOUND", "Sesión de inventario no encontrada"));
        }

        if (session.Estado is not (EstadoInventario.Abierta or EstadoInventario.EnProceso))
        {
            return Result.Failure<ConfirmInventarioSesionResponse>(
                Error.Validation("SESION_NO_CONFIRMABLE", "Solo se pueden confirmar sesiones en estado Abierta o EnProceso"));
        }

        if (!session.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure<ConfirmInventarioSesionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La sesión fue modificada por otro usuario. Recargue e intente nuevamente."));
        }

        // PHASE 1 — pre-validate. No writes: a negative count can never become a valid stock.
        // Since CantidadContada IS the reconciled stock, it can't drive stock negative by
        // definition — the only guard needed is rejecting negative counts.
        foreach (var conteo in session.Conteos.Where(c => c.Diferencia != 0))
        {
            if (conteo.CantidadContada < 0)
            {
                return Result.Failure<ConfirmInventarioSesionResponse>(
                    Error.Validation("CANTIDAD_NEGATIVA",
                        "La cantidad contada no puede ser negativa para una línea con diferencia"));
            }
        }

        var insumoIds = session.Conteos
            .Where(c => c.Diferencia != 0 && c.InsumoId.HasValue)
            .Select(c => c.InsumoId!.Value)
            .Distinct()
            .ToList();
        var ptIds = session.Conteos
            .Where(c => c.Diferencia != 0 && c.ProductoTerminadoId.HasValue)
            .Select(c => c.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();

        var insumoDict = (await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken)).ToDictionary(i => i.Id);
        // Tracked PT load: StockActual is overwritten below; AsNoTracking would silently drop it.
        var ptDict = (await _productoTerminadoRepository.GetTrackedByIdsAsync(ptIds, cancellationToken)).ToDictionary(p => p.Id);

        // PHASE 2 — writes. The stock logic is inlined from RegisterMovementCommandHandler
        // (which commits per call and would break the all-or-nothing close): StockActual is set
        // to the reconciled count and one Ajuste movement mirrors its signed quantity.
        var ajustesGenerados = 0;
        var diferenciaTotal = 0m;
        foreach (var conteo in session.Conteos.Where(c => c.Diferencia != 0))
        {
            var tipo = conteo.CantidadContada > conteo.CantidadSistema
                ? TipoMovimientoStock.AjustePositivo
                : TipoMovimientoStock.AjusteNegativo;
            var cantidad = conteo.CantidadContada - conteo.CantidadSistema;

            if (conteo.InsumoId.HasValue)
            {
                if (!insumoDict.TryGetValue(conteo.InsumoId.Value, out var insumo))
                {
                    return Result.Failure<ConfirmInventarioSesionResponse>(
                        Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {conteo.InsumoId.Value} no encontrado"));
                }

                insumo.StockActual = conteo.CantidadContada;

                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = insumo.Id,
                    ProductoTerminadoId = null,
                    Tipo = tipo,
                    Cantidad = cantidad,
                    CantidadOriginal = Math.Abs(cantidad),
                    UnidadOriginalId = insumo.UnidadConsumoId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = insumo.PrecioUltimaCompra,
                    Motivo = $"Inventario {session.Id:N}",
                    DocumentoOrigen = session.Id.ToString(),
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }
            else
            {
                if (!ptDict.TryGetValue(conteo.ProductoTerminadoId!.Value, out var pt))
                {
                    return Result.Failure<ConfirmInventarioSesionResponse>(
                        Error.NotFound("PRODUCTO_TERMINADO_NOT_FOUND", $"Producto terminado {conteo.ProductoTerminadoId!.Value} no encontrado"));
                }

                pt.StockActual = conteo.CantidadContada;

                await _movimientoStockRepository.AddAsync(new MovimientoStock
                {
                    Id = Guid.NewGuid(),
                    InsumoId = null,
                    ProductoTerminadoId = pt.Id,
                    Tipo = tipo,
                    Cantidad = cantidad,
                    CantidadOriginal = Math.Abs(cantidad),
                    UnidadOriginalId = pt.UnidadMedidaId,
                    FactorConversionAplicado = 1,
                    PrecioUnitario = await _costoResolver.CalcularPorRecetaAsync(pt.RecetaId, cancellationToken),
                    Motivo = $"Inventario {session.Id:N}",
                    DocumentoOrigen = session.Id.ToString(),
                    UsuarioId = _currentUser.UsuarioId!.Value,
                    Fecha = RelojDeNegocio.Ahora
                }, cancellationToken);
            }

            ajustesGenerados++;
            diferenciaTotal += Math.Abs(cantidad);
        }

        session.Estado = EstadoInventario.Cerrada;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<ConfirmInventarioSesionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La sesión fue modificada por otro usuario. Reintente."));
        }

        return new ConfirmInventarioSesionResponse(session.Id, session.Estado, ajustesGenerados, diferenciaTotal);
    }
}
