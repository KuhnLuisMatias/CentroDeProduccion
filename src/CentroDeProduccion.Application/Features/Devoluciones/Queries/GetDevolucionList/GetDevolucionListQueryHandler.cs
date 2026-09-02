using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Devoluciones.Queries;
using CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionById;

namespace CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionList;

public class GetDevolucionListQueryHandler
{
    private readonly IDevolucionRepository _devolucionRepository;

    public GetDevolucionListQueryHandler(IDevolucionRepository devolucionRepository)
    {
        _devolucionRepository = devolucionRepository;
    }

    public async Task<Result<IReadOnlyList<DevolucionListItemResponse>>> HandleAsync(
        GetDevolucionListQuery query, CancellationToken cancellationToken = default)
    {
        var devoluciones = await _devolucionRepository.GetByFiltersAsync(
            query.RemitoId, query.BarId, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = devoluciones.Select(GetDevolucionByIdQueryHandler.MapListItem).ToList();
        return Result.Success<IReadOnlyList<DevolucionListItemResponse>>(response);
    }
}