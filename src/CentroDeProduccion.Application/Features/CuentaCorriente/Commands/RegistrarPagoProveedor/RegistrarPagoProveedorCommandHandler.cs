using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegistrarPagoProveedor;

/// <summary>
/// Registers a payment to a supplier (append-only): computes the total server-side from the
/// payment method lines, optionally validates it against the factura's outstanding balance
/// (Σ negative CC rows linked to the factura; allows partial payment, rejects overpayment with
/// PAGO_EXCEDE_PENDIENTE), then writes ONE negative CuentaCorrienteProveedor row
/// (TipoMovimiento.Pago) plus the PagoAProveedor aggregate with its method lines in a single
/// SaveChangesAsync. The settled amount per factura is always derived from the ledger.
/// </summary>
public class RegistrarPagoProveedorCommandHandler
{
    private readonly IPagoAProveedorRepository _pagoAProveedorRepository;
    private readonly IPagoProveedorRepository _pagoProveedorRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RegistrarPagoProveedorCommand> _validator;

    public RegistrarPagoProveedorCommandHandler(
        IPagoAProveedorRepository pagoAProveedorRepository,
        IPagoProveedorRepository pagoProveedorRepository,
        IProveedorRepository proveedorRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<RegistrarPagoProveedorCommand> validator)
    {
        _pagoAProveedorRepository = pagoAProveedorRepository;
        _pagoProveedorRepository = pagoProveedorRepository;
        _proveedorRepository = proveedorRepository;
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<RegistrarPagoProveedorResponse>> HandleAsync(
        RegistrarPagoProveedorCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegistrarPagoProveedorResponse>(errors.First());
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.ProveedorId, cancellationToken);
        if (proveedor == null || !proveedor.Activo)
        {
            return Result.Failure<RegistrarPagoProveedorResponse>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado o inactivo"));
        }

        // Server-computed total: the client never supplies MontoTotal.
        var montoTotal = command.Medios.Sum(m => m.Monto);

        Domain.Entities.PagoProveedor? factura = null;
        decimal pagadoPrevio = 0m;
        if (command.FacturaId.HasValue)
        {
            factura = await _pagoProveedorRepository.GetByIdAsync(command.FacturaId.Value, cancellationToken);
            if (factura == null)
            {
                return Result.Failure<RegistrarPagoProveedorResponse>(
                    Error.NotFound("FACTURA_NOT_FOUND", "Factura no encontrada"));
            }
            if (factura.ProveedorId != proveedor.Id)
            {
                return Result.Failure<RegistrarPagoProveedorResponse>(
                    Error.Validation("FACTURA_OTRO_PROVEEDOR", "La factura no pertenece al proveedor indicado"));
            }

            pagadoPrevio = await _cuentaCorrienteRepository.GetPagosAplicadosAFacturaAsync(factura.Id, cancellationToken);
            var pendiente = factura.MontoTotal - pagadoPrevio;
            if (montoTotal > pendiente)
            {
                return Result.Failure<RegistrarPagoProveedorResponse>(
                    Error.Validation("PAGO_EXCEDE_PENDIENTE",
                        $"El pago ({montoTotal:N2}) excede el pendiente de la factura ({pendiente:N2} de {factura.MontoTotal:N2}; ya pagado: {pagadoPrevio:N2})"));
            }
        }

        var referencia = factura != null
            ? $"Pago factura N° {factura.Numero}"
            : (!string.IsNullOrWhiteSpace(command.Observaciones) ? command.Observaciones : "Pago a cuenta");

        var pago = new PagoAProveedor
        {
            Id = Guid.NewGuid(),
            ProveedorId = proveedor.Id,
            Fecha = command.Fecha,
            MontoTotal = montoTotal,
            Observaciones = command.Observaciones,
            CreadoPor = _currentUser.UsuarioId!.Value,
            FechaCreacion = RelojDeNegocio.Ahora,
            FacturaId = factura?.Id,
            Medios = command.Medios.Select(m => new PagoAProveedorMetodo
            {
                Id = Guid.NewGuid(),
                Tipo = (MetodoPago)m.Tipo,
                Monto = m.Monto,
                Referencia = m.Referencia
            }).ToList()
        };

        await _pagoAProveedorRepository.AddAsync(pago, cancellationToken);

        await _cuentaCorrienteRepository.AddAsync(new CuentaCorrienteProveedor
        {
            Id = Guid.NewGuid(),
            ProveedorId = proveedor.Id,
            TipoMovimiento = TipoMovimientoCtaCte.Pago,
            Monto = -montoTotal,
            Referencia = referencia,
            Fecha = command.Fecha,
            PagoProveedorId = factura?.Id,
            FechaCreacion = RelojDeNegocio.Ahora
        }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<RegistrarPagoProveedorResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El pago fue modificado por otro usuario. Reintente."));
        }

        if (factura == null)
        {
            return new RegistrarPagoProveedorResponse(pago.Id, proveedor.Id, pago.MontoTotal, pago.Fecha, null, null, null, null);
        }

        var pagadoFinal = pagadoPrevio + montoTotal;
        return new RegistrarPagoProveedorResponse(
            pago.Id, proveedor.Id, pago.MontoTotal, pago.Fecha, factura.Id, factura.MontoTotal,
            pagadoFinal, Math.Max(0m, factura.MontoTotal - pagadoFinal));
    }
}
