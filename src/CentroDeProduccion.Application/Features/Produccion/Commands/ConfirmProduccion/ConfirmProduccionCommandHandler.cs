using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;

/// <summary>
/// Confirms a Borrador production run (Producción simple): deducts exactly the edited
/// <c>InsumosConsumidos</c> lines from insumo stock, find-or-creates the finished product
/// derived from the recipe name, writes one internal ProduccionSalida row (report/report
/// compatibility), computes cost as Σ real consumption ÷ declared output, and increments
/// finished-product stock with lot and unit cost. All inside one UnitOfWork transaction.
/// The declared <see cref="ConfirmProduccionCommand.CantidadProducida"/> is authoritative; no
/// validation against theoretical yield.
/// </summary>
public class ConfirmProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IRecetaRepository _recetaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ConfirmProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IRecetaRepository recetaRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        IUnidadMedidaRepository unidadMedidaRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _produccionRepository = produccionRepository;
        _recetaRepository = recetaRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
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

        // Batch-load every consumed insumo up-front (tracked: StockActual mutates below).
        var lineas = produccion.InsumosConsumidos.ToList();
        var insumos = (await _insumoRepository.GetByIdsAsync(
                lineas.Select(l => l.InsumoId).Distinct().ToList(), cancellationToken))
            .ToDictionary(i => i.Id);

        foreach (var linea in lineas)
        {
            if (!insumos.TryGetValue(linea.InsumoId, out var insumo))
            {
                return Result.Failure<ConfirmProduccionResponse>(Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {linea.InsumoId} no encontrado"));
            }
        }

        // Stock is allowed to go negative: deduct and ledger each consumption regardless.
        foreach (var linea in lineas)
        {
            var insumo = insumos[linea.InsumoId];
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

        // Costeo over REAL consumption only (edited lines), not the template.
        var costoTotalInsumos = lineas.Sum(l => insumos[l.InsumoId].PrecioUltimaCompra * l.Cantidad);
        var costoUnitario = costoTotalInsumos / command.CantidadProducida;

        // Single internal salida row keeps rentabilidad/reports reading Salidas working.
        // No explicit Id: graph-discovered through the tracked parent (preset key would be
        // treated as an existing row).
        produccion.Salidas.Add(new ProduccionSalida
        {
            ProductoTerminadoId = producto.Id,
            Cantidad = command.CantidadProducida,
            CostoUnitario = costoUnitario,
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
