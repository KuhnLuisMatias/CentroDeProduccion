using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;

/// <summary>
/// Appends a NotaCredito movement (negative Monto, reduces the supplier's debt). The ledger is
/// append-only — no edit or delete is ever possible.
/// </summary>
public class RegisterNotaCreditoCommandHandler
{
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterNotaCreditoCommand> _validator;

    public RegisterNotaCreditoCommandHandler(
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IProveedorRepository proveedorRepository,
        IUnitOfWork unitOfWork,
        IValidator<RegisterNotaCreditoCommand> validator)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _proveedorRepository = proveedorRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result<RegisterNotaCreditoResponse>> HandleAsync(
        RegisterNotaCreditoCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => Error.Validation(e.PropertyName, e.ErrorMessage)).ToList();
            return Result.Failure<RegisterNotaCreditoResponse>(errors.First());
        }

        var proveedor = await _proveedorRepository.GetByIdAsync(command.ProveedorId, cancellationToken);
        if (proveedor == null)
        {
            return Result.Failure<RegisterNotaCreditoResponse>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado"));
        }

        var movimiento = new CuentaCorrienteProveedor
        {
            Id = Guid.NewGuid(),
            ProveedorId = proveedor.Id,
            TipoMovimiento = TipoMovimientoCtaCte.NotaCredito,
            Monto = -command.Monto,
            Referencia = command.Referencia,
            Fecha = RelojDeNegocio.Ahora,
            FechaCreacion = RelojDeNegocio.Ahora
        };

        await _cuentaCorrienteRepository.AddAsync(movimiento, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterNotaCreditoResponse(
            movimiento.Id, proveedor.Id, movimiento.TipoMovimiento, movimiento.Monto, movimiento.Fecha, movimiento.Referencia);
    }
}