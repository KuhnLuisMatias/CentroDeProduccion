using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaDebito;

/// <summary>
/// Appends a NotaDebito movement (positive Monto, adds to the bar's debt). The ledger is
/// append-only — no edit or delete is ever possible.
/// </summary>
public class RegisterNotaDebitoCommandHandler
{
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteRepository;
    private readonly IBarRepository _barRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterNotaDebitoCommand> _validator;

    public RegisterNotaDebitoCommandHandler(
        ICuentaCorrienteBarRepository cuentaCorrienteRepository,
        IBarRepository barRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegisterNotaDebitoCommand> validator)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _barRepository = barRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RegisterNotaDebitoResponse>> HandleAsync(
        RegisterNotaDebitoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegisterNotaDebitoResponse>(errors.First());
        }

        var bar = await _barRepository.GetByIdAsync(command.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<RegisterNotaDebitoResponse>(
                Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (bar.Estado != EstadoBar.Activo)
        {
            return Result.Failure<RegisterNotaDebitoResponse>(
                Error.Validation("BAR_INACTIVO", "No se puede registrar un movimiento para un bar inactivo"));
        }

        if (command.Monto < 0)
        {
            return Result.Failure<RegisterNotaDebitoResponse>(
                Error.Validation("MONTO_INVALIDO", "La nota de débito debe tener monto positivo"));
        }

        var movimiento = new CentroDeProduccion.Domain.Entities.CuentaCorrienteBar
        {
            Id = Guid.NewGuid(),
            BarId = bar.Id,
            TipoMovimiento = TipoMovimientoCtaCteBar.NotaDebito,
            Monto = command.Monto,
            Referencia = command.Referencia,
            Fecha = command.Fecha ?? RelojDeNegocio.Ahora,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _cuentaCorrienteRepository.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterNotaDebitoResponse(
            movimiento.Id, bar.Id, movimiento.TipoMovimiento, movimiento.Monto, movimiento.Fecha, movimiento.Referencia);
    }
}