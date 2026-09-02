using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;

/// <summary>
/// Creates a remito in Pendiente state for an active bar with a sequential unique NumeroRemito.
/// Each line is priced as a snapshot: finished products at their on-the-fly recipe BOM cost
/// (see ProductoTerminadoCostoResolver), inputs at the weighted-average price marked up by the
/// bar's resale margin.
/// </summary>
public class CreateRemitoCommandHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IBarRepository _barRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateRemitoCommand> _validator;

    public CreateRemitoCommandHandler(
        IRemitoRepository remitoRepository,
        IBarRepository barRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IInsumoRepository insumoRepository,
        ProductoTerminadoCostoResolver costoResolver,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateRemitoCommand> validator)
    {
        _remitoRepository = remitoRepository;
        _barRepository = barRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _insumoRepository = insumoRepository;
        _costoResolver = costoResolver;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreateRemitoResponse>> HandleAsync(CreateRemitoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreateRemitoResponse>(errors.First());
        }

        var bar = await _barRepository.GetByIdAsync(command.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<CreateRemitoResponse>(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<CreateRemitoResponse>(
                Error.Validation("BAR_INACTIVO", "No se puede crear un remito para un bar inactivo"));
        }

        var lineasResult = await BuildLineasAsync(command.Lineas, bar, cancellationToken);
        if (lineasResult.IsFailure)
        {
            return Result.Failure<CreateRemitoResponse>(lineasResult.Error);
        }

        var numero = await _remitoRepository.GetNextNumeroAsync(cancellationToken);

        var remito = new Remito
        {
            Id = Guid.NewGuid(),
            NumeroRemito = numero,
            BarId = bar.Id,
            Estado = EstadoRemito.Pendiente,
            Observaciones = command.Observaciones,
            EntregadoPor = command.EntregadoPor,
            RecibidoPor = command.RecibidoPor,
            FechaCreacion = RelojDeNegocio.Ahora,
            CreadoPor = _currentUser.UsuarioId!.Value,
            Lineas = lineasResult.Value
        };

        await _remitoRepository.AddAsync(remito, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreateRemitoResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Reintente."));
        }

        var total = remito.Lineas.Sum(l => l.Subtotal);
        return new CreateRemitoResponse(remito.Id, remito.NumeroRemito, remito.Estado, total);
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