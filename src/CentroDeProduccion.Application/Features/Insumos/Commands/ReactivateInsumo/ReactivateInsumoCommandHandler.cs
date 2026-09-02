using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Insumos.Commands.ReactivateInsumo;

public class ReactivateInsumoCommandHandler
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateInsumoCommandHandler(IInsumoRepository insumoRepository, IUnitOfWork unitOfWork)
    {
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(ReactivateInsumoCommand command, CancellationToken cancellationToken = default)
    {
        var insumo = await _insumoRepository.GetByIdAsync(command.Id, cancellationToken);
        if (insumo == null)
        {
            return Result.Failure(Error.NotFound("INSUMO_NOT_FOUND", "Insumo no encontrado"));
        }

        if (insumo.Activo)
        {
            return Result.Failure(Error.Validation("INSUMO_YA_ACTIVO", "El insumo ya está activo"));
        }

        insumo.Activo = true;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(
                Error.Concurrency("CONCURRENCY_CONFLICT", "El insumo fue modificado por otro usuario. Recargue e intente nuevamente."));
        }

        return Result.Success();
    }
}
