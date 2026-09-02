using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterCompensacion;

/// <summary>
/// Appends a Compensacion movement (Monto may be positive or negative, stored as given). The
/// ledger is append-only — no edit or delete is ever possible.
/// </summary>
public class RegisterCompensacionCommandHandler
{
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteRepository;
    private readonly IBarRepository _barRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterCompensacionCommand> _validator;

    public RegisterCompensacionCommandHandler(
        ICuentaCorrienteBarRepository cuentaCorrienteRepository,
        IBarRepository barRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegisterCompensacionCommand> validator)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _barRepository = barRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RegisterCompensacionResponse>> HandleAsync(
        RegisterCompensacionCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegisterCompensacionResponse>(errors.First());
        }

        var bar = await _barRepository.GetByIdAsync(command.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<RegisterCompensacionResponse>(
                Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<RegisterCompensacionResponse>(
                Error.Validation("BAR_INACTIVO", "No se puede registrar un movimiento para un bar inactivo"));
        }

        var movimiento = new CentroDeProduccion.Domain.Entities.CuentaCorrienteBar
        {
            Id = Guid.NewGuid(),
            BarId = bar.Id,
            TipoMovimiento = TipoMovimientoCtaCteBar.Compensacion,
            Monto = command.Monto,
            Referencia = command.Referencia,
            Fecha = command.Fecha ?? RelojDeNegocio.Ahora,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _cuentaCorrienteRepository.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterCompensacionResponse(
            movimiento.Id, bar.Id, movimiento.TipoMovimiento, movimiento.Monto, movimiento.Fecha, movimiento.Referencia);
    }
}