using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.UpdateEstadoRemito;

/// <summary>
/// Transitions a remito between Pendiente and EnProceso only. The Enviado state is reached
/// exclusively through ConfirmRemito and Cancelado through CancelarRemito, so neither can be
/// set here. A RowVersion mismatch rejects the request with 409.
/// </summary>
public class UpdateEstadoRemitoCommandHandler
{
    private readonly IRemitoRepository _remitoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEstadoRemitoCommandHandler(
        IRemitoRepository remitoRepository,
        IUnitOfWork unitOfWork)
    {
        _remitoRepository = remitoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateEstadoRemitoCommand command, CancellationToken cancellationToken = default)
    {
        var remito = await _remitoRepository.GetByIdAsync(command.RemitoId, cancellationToken);
        if (remito == null)
        {
            return Result.Failure(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        if (!remito.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El remito fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        var esTransicionValida =
            (remito.Estado == EstadoRemito.Pendiente && command.Estado == EstadoRemito.EnProceso) ||
            (remito.Estado == EstadoRemito.EnProceso && command.Estado == EstadoRemito.Pendiente);

        if (!esTransicionValida)
        {
            return Result.Failure(
                Error.Validation("ESTADO_TRANSICION_INVALIDA", "Transición de estado no permitida"));
        }

        remito.Estado = command.Estado;

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