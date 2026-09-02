using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;

/// <summary>
/// Processes a payment from a bar. The sum of the payment methods and the sum of the
/// allocations must both equal MontoTotal, and no allocation may exceed the outstanding
/// debt of its remito. One CuentaCorrienteBar Pago movement (-MontoTotal) is created
/// atomically with the payment.
/// </summary>
public class CreatePagoBarCommandHandler
{
    private readonly IPagoBarRepository _pagoBarRepository;
    private readonly IBarRepository _barRepository;
    private readonly IRemitoRepository _remitoRepository;
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreatePagoBarCommand> _validator;

    public CreatePagoBarCommandHandler(
        IPagoBarRepository pagoBarRepository,
        IBarRepository barRepository,
        IRemitoRepository remitoRepository,
        ICuentaCorrienteBarRepository cuentaCorrienteBarRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreatePagoBarCommand> validator)
    {
        _pagoBarRepository = pagoBarRepository;
        _barRepository = barRepository;
        _remitoRepository = remitoRepository;
        _cuentaCorrienteBarRepository = cuentaCorrienteBarRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<CreatePagoBarResponse>> HandleAsync(
        CreatePagoBarCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<CreatePagoBarResponse>(errors.First());
        }

        var bar = await _barRepository.GetByIdAsync(command.BarId, cancellationToken);
        if (bar == null || bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<CreatePagoBarResponse>(
                Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado o inactivo"));
        }

        var sumaMetodos = command.Metodos.Sum(m => m.Monto);
        if (sumaMetodos != command.MontoTotal)
        {
            return Result.Failure<CreatePagoBarResponse>(
                Error.Validation("ASIGNACION_INVALIDA",
                    $"La suma de los métodos de pago ({sumaMetodos}) no coincide con el monto total ({command.MontoTotal})"));
        }

        var sumaAsignaciones = command.Items.Sum(i => i.MontoAplicado);
        if (sumaAsignaciones != command.MontoTotal)
        {
            return Result.Failure<CreatePagoBarResponse>(
                Error.Validation("ASIGNACION_INVALIDA",
                    $"La suma de las asignaciones ({sumaAsignaciones}) no coincide con el monto total ({command.MontoTotal})"));
        }

        foreach (var item in command.Items)
        {
            var remito = await _remitoRepository.GetByIdWithLineasAsync(item.RemitoId, cancellationToken);
            if (remito == null)
            {
                return Result.Failure<CreatePagoBarResponse>(
                    Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
            }

            if (remito.Estado != EstadoRemito.Enviado)
            {
                return Result.Failure<CreatePagoBarResponse>(
                    Error.Validation("REMITO_ESTADO_INVALIDO", "Solo se pueden asignar pagos a remitos enviados"));
            }

            var totalRemito = remito.Lineas.Sum(l => l.Subtotal);
            var yaPagado = await _pagoBarRepository.GetTotalPaidForRemitoAsync(remito.Id, cancellationToken);
            var devuelto = await _cuentaCorrienteBarRepository.GetDevolucionTotalByRemitoAsync(remito.Id, cancellationToken);
            var deudaPendiente = totalRemito - yaPagado - devuelto;

            if (item.MontoAplicado > deudaPendiente)
            {
                return Result.Failure<CreatePagoBarResponse>(
                    Error.Validation("ASIGNACION_EXCEDE_DEUDA",
                        $"La asignación ({item.MontoAplicado}) excede la deuda pendiente del remito ({deudaPendiente})"));
            }
        }

        var numero = await _pagoBarRepository.GetNextNumeroAsync(cancellationToken);
        var fechaPago = command.FechaPago ?? RelojDeNegocio.Ahora;

        var pagoBar = new PagoBar
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            BarId = bar.Id,
            FechaPago = fechaPago,
            MontoTotal = command.MontoTotal,
            Observaciones = command.Observaciones,
            CreadoPor = _currentUser.UsuarioId!.Value,
            FechaCreacion = RelojDeNegocio.Ahora,
            Metodos = command.Metodos.Select(m => new PagoBarMetodo
            {
                Tipo = m.Tipo,
                Monto = m.Monto,
                Referencia = m.Referencia
            }).ToList(),
            Items = command.Items.Select(i => new PagoBarItem
            {
                Id = Guid.NewGuid(),
                RemitoId = i.RemitoId,
                MontoAplicado = i.MontoAplicado
            }).ToList()
        };

        await _pagoBarRepository.AddAsync(pagoBar, cancellationToken);

        await _cuentaCorrienteBarRepository.AddAsync(new CentroDeProduccion.Domain.Entities.CuentaCorrienteBar
        {
            Id = Guid.NewGuid(),
            BarId = bar.Id,
            TipoMovimiento = TipoMovimientoCtaCteBar.Pago,
            Monto = -command.MontoTotal,
            Referencia = $"PagoBar #{numero}",
            PagoBarId = pagoBar.Id,
            Fecha = fechaPago,
            FechaCreacion = RelojDeNegocio.Ahora
        }, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure<CreatePagoBarResponse>(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El pago fue modificado por otro usuario. Reintente."));
        }

        return new CreatePagoBarResponse(pagoBar.Id, pagoBar.Numero, bar.Id, pagoBar.MontoTotal, pagoBar.FechaPago);
    }
}