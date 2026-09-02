using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.DeactivateUnidadMedida;

public class DeactivateUnidadMedidaCommandHandler
{
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUnidadMedidaCommandHandler(
        IUnidadMedidaRepository unidadMedidaRepository,
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork)
    {
        _unidadMedidaRepository = unidadMedidaRepository;
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeactivateUnidadMedidaCommand command, CancellationToken cancellationToken = default)
    {
        var unidad = await _unidadMedidaRepository.GetByIdAsync(command.Id, cancellationToken);
        if (unidad == null)
        {
            return Result.Failure(Error.NotFound("UNIDAD_NOT_FOUND", "Unidad de medida no encontrada"));
        }

        if (await _insumoRepository.ExistsUsingUnidadMedidaAsync(unidad.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("UNIDAD_EN_USO", "La unidad está siendo usada por uno o más insumos"));
        }

        unidad.Activo = false;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
