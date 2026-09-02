using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.CancelarRemito;

/// <summary>
/// Cancels a remito from Pendiente or EnProceso. Cancellation is terminal and does not touch
/// stock or CuentaCorriente (those side effects only occur when the remito is sent via
/// ConfirmRemito). A RowVersion mismatch rejects the request with 409.
/// </summary>
public class CancelarRemitoCommandHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelarRemitoCommandHandler(
        IRemitoRepository remitoRepository,
        IUnitOfWork unitOfWork)
    {
        _remitoRepository = remitoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(CancelarRemitoCommand command, CancellationToken cancellationToken = default)
    {
        var remito = await _remitoRepository.GetByIdAsync(command.RemitoId, cancellationToken);
        if (remito == null)
        {
            return Result.Failure(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        if (remito.Estado is not (EstadoRemito.Pendiente or EstadoRemito.EnProceso))
        {
            return Result.Failure(
                Error.Validation("REMITO_NO_CANCELABLE", "Solo se pueden cancelar remitos en estado Pendiente o EnProceso"));
        }

        if (!remito.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        remito.Estado = EstadoRemito.Cancelado;

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
}