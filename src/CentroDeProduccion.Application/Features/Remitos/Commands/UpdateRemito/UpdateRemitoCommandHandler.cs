using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.UpdateRemito;

/// <summary>
/// Edits the header and replaces the lines of a remito. Only remitos in Pendiente or EnProceso
/// state are editable; lines are re-priced with the same snapshot logic used on create. A
/// RowVersion mismatch rejects the request with 409.
/// </summary>
public class UpdateRemitoCommandHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IBarRepository _barRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateRemitoCommand> _validator;

    public UpdateRemitoCommandHandler(
        IRemitoRepository remitoRepository,
        IBarRepository barRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IInsumoRepository insumoRepository,
        ProductoTerminadoCostoResolver costoResolver,
        IUnitOfWork unitOfWork,
        IValidator<UpdateRemitoCommand> validator)
    {
        _remitoRepository = remitoRepository;
        _barRepository = barRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _insumoRepository = insumoRepository;
        _costoResolver = costoResolver;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> HandleAsync(UpdateRemitoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure(errors.First());
        }

        var remito = await _remitoRepository.GetByIdWithLineasAsync(command.Id, cancellationToken);
        if (remito == null)
        {
            return Result.Failure(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        if (remito.Estado == EstadoRemito.Enviado)
        {
            return Result.Failure(
                Error.Validation("REMITO_NO_EDITABLE", "No se puede editar un remito en estado Enviado"));
        }

        if (remito.Estado == EstadoRemito.Cancelado)
        {
            return Result.Failure(
                Error.Validation("REMITO_NO_EDITABLE", "No se puede editar un remito en estado Cancelado"));
        }

        if (!remito.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        var bar = await _barRepository.GetByIdAsync(command.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure(
                Error.Validation("BAR_INACTIVO", "No se puede crear un remito para un bar inactivo"));
        }

        var lineasResult = await BuildLineasAsync(command.Lineas, bar, cancellationToken);
        if (lineasResult.IsFailure)
        {
            return Result.Failure(lineasResult.Error);
        }

        remito.BarId = bar.Id;
        remito.Observaciones = command.Observaciones;
        remito.EntregadoPor = command.EntregadoPor;
        remito.RecibidoPor = command.RecibidoPor;

        remito.Lineas.Clear();
        foreach (var linea in lineasResult.Value)
        {
            remito.Lineas.Add(linea);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        return Result.Success();
    }

    private async Task<Result<List<RemitoLinea>>> BuildLineasAsync(
        IReadOnlyList<CreateRemitoLineaCommand> items, Bar bar, CancellationToken cancellationToken)
    {
        var lineas = new List<RemitoLinea>();

        // Batch-load all referenced products/inputs up-front: one query per type instead of
        // one query per remito line (N+1).
        var productoIds = items
            .Where(i => i.TipoLinea == TipoLineaRemito.ProductoTerminado)
            .Select(i => i.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();
        var insumoIds = items
            .Where(i => i.TipoLinea != TipoLineaRemito.ProductoTerminado)
            .Select(i => i.InsumoId!.Value)
            .Distinct()
            .ToList();

        var productosDict = (await _productoTerminadoRepository.GetByIdsAsync(productoIds, cancellationToken))
            .ToDictionary(p => p.Id);
        var insumosDict = (await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken))
            .ToDictionary(i => i.Id);

        foreach (var item in items)
        {
            decimal precioUnitario;

            if (item.TipoLinea == TipoLineaRemito.ProductoTerminado)
            {
                if (!productosDict.TryGetValue(item.ProductoTerminadoId!.Value, out var productoTerminado))
                {
                    return Result.Failure<List<RemitoLinea>>(
                        Error.NotFound("PRODUCTO_TERMINADO_NOT_FOUND", $"Producto terminado {item.ProductoTerminadoId} no encontrado"));
                }

                precioUnitario = await _costoResolver.CalcularPorRecetaAsync(productoTerminado.RecetaId, cancellationToken);
            }
            else
            {
                if (!insumosDict.TryGetValue(item.InsumoId!.Value, out var insumo))
                {
                    return Result.Failure<List<RemitoLinea>>(
                        Error.NotFound("INSUMO_NOT_FOUND", $"Insumo {item.InsumoId} no encontrado"));
                }

                precioUnitario = Math.Round(
                    insumo.PrecioUltimaCompra * (1 + bar.MargenReventaPorcentaje / 100), 4);
            }

            lineas.Add(new RemitoLinea
            {
                Id = Guid.NewGuid(),
                TipoLinea = item.TipoLinea,
                ProductoTerminadoId = item.ProductoTerminadoId,
                InsumoId = item.InsumoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = item.Cantidad * precioUnitario,
                Lote = item.Lote,
                Observaciones = item.Observaciones
            });
        }

        return Result.Success(lineas);
    }
}