using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;

/// <summary>
/// Confirms a Borrador production run (Producción simple): deducts exactly the edited
/// <c>InsumosConsumidos</c> lines — direct insumos from insumo stock, sub-recipe lines from the
/// active finished product whose <c>RecetaId</c> matches the sub-recipe (missing/inactive PT or
/// insufficient stock FAILS the confirmation; no lazy creation) —, find-or-creates the finished
/// product derived from the recipe name, writes one internal ProduccionSalida row, computes cost
/// as Σ real consumption (insumos at last purchase price + sub-PTs at their live standard cost)
/// ÷ declared output, and increments finished-product stock with lot. All inside one UnitOfWork
/// transaction. The declared <see cref="ConfirmProduccionCommand.CantidadProducida"/> is
/// authoritative; no validation against theoretical yield.
/// </summary>
public class ConfirmProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly ProductoTerminadoCostoResolver _productoTerminadoCostoResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ConfirmProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        IUnidadMedidaRepository unidadMedidaRepository,
        ProductoTerminadoCostoResolver productoTerminadoCostoResolver,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _produccionRepository = produccionRepository;
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
        _productoTerminadoCostoResolver = productoTerminadoCostoResolver;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<ConfirmProduccionResponse>> HandleAsync(ConfirmProduccionCommand command, CancellationToken cancellationToken = default)
    {
        // Load WITH InsumosConsumidos: those edited lines are what gets deducted.
        var produccion = await _produccionRepository.GetByIdWithSalidasAsync(command.ProduccionId, cancellationToken);
        if (produccion == null)
        {
            return Result.Failure<ConfirmProduccionResponse>(Error.NotFound("PRODUCCION_NOT_FOUND", "Producción no encontrada"));
        }

        if (produccion.Estado != EstadoProduccion.Borrador)
        {
            return Result.Failure<ConfirmProduccionResponse>(
                Error.Conflict("PRODUCCION_YA_CONFIRMADA", "La producción ya fue confirmada o cancelada"));
        }

        if (!produccion.RowVersion.SequenceEqual(command.RowVersion ?? Array.Empty<byte>()))
        {
            return Result.Failure<ConfirmProduccionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La producción fue modificada por otro usuario. Recargue e intente nuevamente."));
        }

        if (command.CantidadProducida <= 0)
        {
            return Result.Failure<ConfirmProduccionResponse>(
                Error.Validation("CANTIDAD_PRODUCIDA_INVALIDA", "La cantidad producida debe ser mayor a cero"));
        }

        if (produccion.InsumosConsumidos.Count == 0)
        {
            return Result.Failure<ConfirmProduccionResponse>(
                Error.Validation("PRODUCCION_SIN_INSUMOS", "La producción no tiene insumos consumidos declarados"));
        }

        var receta = await _recetaRepository.GetByIdAsync(produccion.RecetaId, cancellationToken);
        if (receta == null)
        {
            return Result.Failure<ConfirmProduccionResponse>(Error.NotFound("RECETA_NOT_FOUND", "Receta no encontrada"));
        }

        var lineas = produccion.InsumosConsumidos.ToList();
        var lineasInsumo = lineas.Where(l => l.InsumoId.HasValue).ToList();
        var lineasReceta = lineas.Where(l => l.RecetaOrigenId.HasValue).ToList();

        // Batch-load every consumed insumo up-front (tracked: StockActual mutates below).
        var insumos = (await _insumoRepository.GetByIdsAsync(
                lineasInsumo.Select(l => l.InsumoId!.Value).Distinct().ToList(), cancellationToken))
            .ToDictionary(i => i.Id);

        foreach (var linea in lineasInsumo)
        {
            if (!insumos.TryGetValue(linea.InsumoId!.Value, out var insumo))
            {
                return Result.Failure<ConfirmProduccionResponse>(Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {linea.InsumoId} no encontrado"));
            }
        }

        // Sub-recipe lines: resolve the subreceta + its active finished product and validate
        // stock BEFORE mutating anything. No lazy creation: the sub-recipe must be produced first.
        var subPtPorReceta = new Dictionary<Guid, ProductoTerminado>();
        foreach (var linea in lineasReceta)
        {
            var subRecetaId = linea.RecetaOrigenId!.Value;
            var subReceta = await _recetaRepository.GetByIdAsync(subRecetaId, cancellationToken);
            if (subReceta == null || !subReceta.Activo)
            {
                return Result.Failure<ConfirmProduccionResponse>(
                    Error.NotFound("RECETA_NOT_FOUND", $"La subreceta {subRecetaId} no existe o está inactiva"));
            }

            var pt = await _productoTerminadoRepository.GetTrackedActiveByRecetaIdAsync(subRecetaId, cancellationToken);
            if (pt == null)
            {
                return Result.Failure<ConfirmProduccionResponse>(Error.Validation(
                    "SUBRECETA_SIN_PT",
                    $"La subreceta {subReceta.Nombre} no tiene producto terminado activo. Prodúzcala primero."));
            }

            if (pt.StockActual < linea.Cantidad)
            {
                return Result.Failure<ConfirmProduccionResponse>(Error.Validation(
                    "SUBRECETA_STOCK_INSUFICIENTE",
                    $"La subreceta {subReceta.Nombre} no tiene producto terminado con stock suficiente (requiere {linea.Cantidad}, disponible {pt.StockActual}). Prodúzcala primero."));
            }

            subPtPorReceta[subRecetaId] = pt;
        }

        // Stock is allowed to go negative: deduct and ledger each consumption regardless.
        foreach (var linea in lineasInsumo)
        {
            var insumo = insumos[linea.InsumoId!.Value];
            insumo.StockActual -= linea.Cantidad;

            await _movimientoStockRepository.AddAsync(new MovimientoStock
            {
                Id = Guid.NewGuid(),
                InsumoId = insumo.Id,
                Tipo = TipoMovimientoStock.ConsumoProduccion,
                Cantidad = -linea.Cantidad,
                CantidadOriginal = linea.Cantidad,
                UnidadOriginalId = insumo.UnidadConsumoId,
                FactorConversionAplicado = insumo.FactorConversion,
                Motivo = $"Consumo en producción {produccion.Id}",
                DocumentoOrigen = produccion.Id.ToString(),
                UsuarioId = _currentUser.UsuarioId!.Value,
                Fecha = RelojDeNegocio.Ahora
            }, cancellationToken);
        }

        // Finished product derived from the recipe: find-or-create by recipe name.
        var producto = await _productoTerminadoRepository.GetByNombreAsync(receta.Nombre.Trim(), cancellationToken);
        if (producto == null)
        {
            // Default counting unit for finished goods created from production.
            var unidad = await _unidadMedidaRepository.GetByNombreAsync("Unidad", cancellationToken);
            if (unidad == null)
            {
                return Result.Failure<ConfirmProduccionResponse>(
                    Error.NotFound("UNIDAD_NOT_FOUND", "No existe la unidad de medida 'Unidad'"));
            }

            producto = new ProductoTerminado
            {
                Id = Guid.NewGuid(),
                Nombre = receta.Nombre.Trim(),
                CodigoSku = await GenerarSkuUnicoAsync(receta.CodigoSku, cancellationToken),
                CategoriaId = receta.CategoriaId,
                UnidadMedidaId = unidad.Id,
                RecetaId = receta.Id,
                StockActual = 0,
                FechaProduccion = RelojDeNegocio.Ahora,
                FechaVencimiento = RelojDeNegocio.Ahora.AddDays(30),
                Lote = string.Empty,
                Estado = EstadoProductoTerminado.Disponible,
                Activo = true,
                FechaCreacion = RelojDeNegocio.Ahora
            };
            await _productoTerminadoRepository.AddAsync(producto, cancellationToken);
        }

        var lote = $"{receta.CodigoSku}-{RelojDeNegocio.Ahora:yyyyMMddHHmmss}";

        // Sub-recipe consumption: deduct each sub-PT's stock and ledger it (remito's PT-outflow
        // movement type — no dedicated PT "consumo" type exists). Cost: the sub-PT's live
        // standard cost (same source remitos price with), added to the run's insumo cost.
        var costoSubPt = 0m;
        foreach (var linea in lineasReceta)
        {
            var pt = subPtPorReceta[linea.RecetaOrigenId!.Value];
            pt.StockActual -= linea.Cantidad;

            var costoUnitario = await _productoTerminadoCostoResolver.CalcularPorRecetaAsync(
                linea.RecetaOrigenId.Value, cancellationToken);
            costoSubPt += costoUnitario * linea.Cantidad;

            await _movimientoStockRepository.AddAsync(new MovimientoStock
            {
                Id = Guid.NewGuid(),
                ProductoTerminadoId = pt.Id,
                Tipo = TipoMovimientoStock.VentaBar,
                Cantidad = -linea.Cantidad,
                CantidadOriginal = linea.Cantidad,
                UnidadOriginalId = pt.UnidadMedidaId,
                FactorConversionAplicado = 1,
                Motivo = $"Consumo producción {lote}",
                DocumentoOrigen = produccion.Id.ToString(),
                UsuarioId = _currentUser.UsuarioId!.Value,
                Fecha = RelojDeNegocio.Ahora
            }, cancellationToken);
        }

        // Costeo over REAL consumption only (edited lines), not the template.
        var costoTotalInsumos = lineasInsumo.Sum(l => insumos[l.InsumoId!.Value].PrecioUltimaCompra * l.Cantidad)
            + costoSubPt;

        // Single internal salida row keeps rentabilidad/reports reading Salidas working.
        // No explicit Id: graph-discovered through the tracked parent (preset key would be
        // treated as an existing row).
        produccion.Salidas.Add(new ProduccionSalida
        {
            ProductoTerminadoId = producto.Id,
            Cantidad = command.CantidadProducida,
            TipoSalida = TipoSalidaProduccion.Primario
        });

        producto.StockActual += command.CantidadProducida;
        producto.RecetaId = receta.Id;
        producto.FechaProduccion = RelojDeNegocio.Ahora;
        producto.Lote = lote;
        producto.Estado = EstadoProductoTerminado.Disponible;

        // Ledger entry for the finished-product inflow.
        await _movimientoStockRepository.AddAsync(new MovimientoStock
        {
            Id = Guid.NewGuid(),
            ProductoTerminadoId = producto.Id,
            ProduccionId = produccion.Id,
            Tipo = TipoMovimientoStock.Produccion,
            Cantidad = command.CantidadProducida,
            CantidadOriginal = command.CantidadProducida,
            UnidadOriginalId = producto.UnidadMedidaId,
            FactorConversionAplicado = 1,
            Motivo = $"Producción {lote}",
            DocumentoOrigen = produccion.Id.ToString(),
            UsuarioId = _currentUser.UsuarioId!.Value,
            Fecha = RelojDeNegocio.Ahora
        }, cancellationToken);

        produccion.Lote = lote;
        produccion.CantidadProducida = command.CantidadProducida;
        produccion.CostoTotalInsumos = costoTotalInsumos;
        produccion.CostoTotal = costoTotalInsumos;
        produccion.FechaVencimiento = RelojDeNegocio.Ahora.AddDays(30);
        produccion.Estado = EstadoProduccion.Confirmada;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<ConfirmProduccionResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "Un insumo fue modificado concurrentemente. Reintente la confirmación."));
        }

        return new ConfirmProduccionResponse(produccion.Id, producto.Id, lote, produccion.Estado);
    }

    private async Task<string> GenerarSkuUnicoAsync(string codigoSkuReceta, CancellationToken cancellationToken)
    {
        var baseSku = $"PT-{codigoSkuReceta}";
        if (!await _productoTerminadoRepository.ExistsWithSkuAsync(baseSku, null, cancellationToken))
        {
            return baseSku;
        }

        do
        {
            baseSku = $"PT-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        } while (await _productoTerminadoRepository.ExistsWithSkuAsync(baseSku, null, cancellationToken));

        return baseSku;
    }
}
