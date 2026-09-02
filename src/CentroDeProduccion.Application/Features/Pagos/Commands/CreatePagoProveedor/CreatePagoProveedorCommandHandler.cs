using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;

/// <summary>
/// Creates a supplier purchase invoice ("Factura de Compra"): the real purchase document.
/// Each insumo line generates a Compra stock movement (updating stock and the last purchase
/// price, same rule as RegisterMovementCommandHandler). MontoTotal is computed
/// internally as the sum of insumo subtotals (that amount is the supplier debt) and is
/// recorded as one CuentaCorrienteProveedor Compra movement (+MontoTotal). The actual
/// payment happens separately in cuenta corriente. The Orden de Compra is referential
/// only — no allocation is recorded.
/// </summary>
public class CreatePagoProveedorCommandHandler
{
    private readonly IPagoProveedorRepository _pagoProveedorRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreatePagoProveedorCommand> _validator;

    public CreatePagoProveedorCommandHandler(
        IPagoProveedorRepository pagoProveedorRepository,
        IProveedorRepository proveedorRepository,
        IInsumoRepository insumoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreatePagoProveedorCommand> validator)
    {
        _pagoProveedorRepository = pagoProveedorRepository;
        _proveedorRepository = proveedorRepository;
        _insumoRepository = insumoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreatePagoProveedorResponse>> HandleAsync(
        CreatePagoProveedorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreatePagoProveedorResponse>(errors.First());
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.ProveedorId, cancellationToken);
        if (proveedor == null || !proveedor.Activo)
        {
            return Result.Failure<CreatePagoProveedorResponse>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado o inactivo"));
        }

        var montoTotal = command.Insumos.Sum(i => i.Cantidad * i.PrecioUnitario);

        var insumoIds = command.Insumos.Select(i => i.InsumoId).Distinct().ToList();
        var insumosExistentes = await _insumoRepository.GetByIdsAsync(insumoIds, cancellationToken);
        var insumosPorId = insumosExistentes.ToDictionary(i => i.Id);

        foreach (var insumoCommand in command.Insumos)
        {
            if (!insumosPorId.TryGetValue(insumoCommand.InsumoId, out var insumo) || !insumo.Activo)
            {
                return Result.Failure<CreatePagoProveedorResponse>(
                    Error.NotFound("INSUMO_NOT_FOUND", "Insumo no encontrado o inactivo"));
            }
        }

        var numero = await _pagoProveedorRepository.GetNextNumeroAsync(cancellationToken);

        var pago = new PagoProveedor
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            ProveedorId = proveedor.Id,
            FechaPago = command.FechaPago,
            MontoTotal = montoTotal,
            Observaciones = command.Observaciones,
            CreadoPor = _currentUser.UsuarioId!.Value,
            FechaCreacion = RelojDeNegocio.Ahora,
            Insumos = command.Insumos.Select(i => new PagoInsumo
            {
                Id = Guid.NewGuid(),
                InsumoId = i.InsumoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList()
        };

        await _pagoProveedorRepository.AddAsync(pago, cancellationToken);

        foreach (var linea in pago.Insumos)
        {
            var insumo = insumosPorId[linea.InsumoId];

            var cantidadConsumo = ConversionUnidades.ToUnidadConsumo(
                linea.Cantidad, insumo.UnidadCompraId, insumo.UnidadCompraId, insumo.UnidadConsumoId, insumo.FactorConversion);

            insumo.StockActual += cantidadConsumo;
            insumo.PrecioUltimaCompra = linea.PrecioUnitario;

            await _movimientoStockRepository.AddAsync(new MovimientoStock
            {
                Id = Guid.NewGuid(),
                InsumoId = insumo.Id,
                Tipo = TipoMovimientoStock.Compra,
                Cantidad = cantidadConsumo,
                CantidadOriginal = linea.Cantidad,
                UnidadOriginalId = insumo.UnidadCompraId,
                FactorConversionAplicado = insumo.FactorConversion,
                PrecioUnitario = linea.PrecioUnitario,
                Motivo = $"Factura {numero}",
                DocumentoOrigen = pago.Id.ToString(),
                UsuarioId = _currentUser.UsuarioId!.Value,
                Fecha = RelojDeNegocio.Ahora
            }, cancellationToken);
        }

        await _cuentaCorrienteRepository.AddAsync(new CuentaCorrienteProveedor
        {
            Id = Guid.NewGuid(),
            ProveedorId = proveedor.Id,
            TipoMovimiento = TipoMovimientoCtaCte.Compra,
            Monto = montoTotal,
            Referencia = $"Factura {numero}",
            Fecha = command.FechaPago,
            PagoProveedorId = pago.Id,
            FechaCreacion = RelojDeNegocio.Ahora
        }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreatePagoProveedorResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "La factura fue modificada por otro usuario. Reintente."));
        }

        return new CreatePagoProveedorResponse(pago.Id, pago.Numero, proveedor.Id, pago.MontoTotal);
    }
}
