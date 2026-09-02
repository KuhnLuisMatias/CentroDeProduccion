using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Bares.Commands.DeleteBar;

/// <summary>
/// Soft-deletes a bar (Estado=Inactivo) instead of removing the row, preserving historical
/// Ventas and Remitos integrity.
/// </summary>
public class DeleteBarCommandHandler
{
    private readonly IBarRepository _barRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBarCommandHandler(IBarRepository barRepository, IUnitOfWork unitOfWork)
    {
        _barRepository = barRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteBarCommand command, CancellationToken cancellationToken = default)
    {
        var bar = await _barRepository.GetByIdAsync(command.Id, cancellationToken);
        if (bar == null)
        {
            return Result.Failure(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        if (!bar.RowVersion.SequenceEqual(command.RowVersion))
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El bar fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        bar.Estado = EstadoBar.Inactivo;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El bar fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        return Result.Success();
    }
}