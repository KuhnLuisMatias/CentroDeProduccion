using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.CancelProduccion;

/// <summary>
/// Cancels a Borrador production run. A confirmed run cannot be cancelled (stock already moved).
/// </summary>
public class CancelProduccionCommandHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelProduccionCommandHandler(
        IProduccionRepository produccionRepository,
        IUnitOfWork unitOfWork)
    {
        _produccionRepository = produccionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CancelProduccionResponse>> HandleAsync(CancelProduccionCommand command, CancellationToken cancellationToken = default)
    {
        var produccion = await _produccionRepository.GetByIdAsync(command.ProduccionId, cancellationToken);
        if (produccion == null)
        {
            return Result.Failure<CancelProduccionResponse>(Error.NotFound("PRODUCCION_NOT_FOUND", "Producción no encontrada"));
        }

        if (produccion.Estado != EstadoProduccion.Borrador)
        {
            return Result.Failure<CancelProduccionResponse>(
                Error.Conflict("PRODUCCION_NO_CANCELABLE", "Solo se puede cancelar una producción en estado borrador"));
        }

        produccion.Estado = EstadoProduccion.Cancelada;
        produccion.Observaciones = string.IsNullOrWhiteSpace(command.Motivo)
            ? produccion.Observaciones
            : $"{produccion.Observaciones}\nCancelada: {command.Motivo}".Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelProduccionResponse(produccion.Id, produccion.Estado);
    }
}
