using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.Queries;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoList;

public class GetRemitoListQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;

    public GetRemitoListQueryHandler(IRemitoRepository remitoRepository)
    {
        _remitoRepository = remitoRepository;
    }

    public async Task<Result<IReadOnlyList<RemitoListItemResponse>>> HandleAsync(
        GetRemitoListQuery query, CancellationToken cancellationToken = default)
    {
        var remitos = await _remitoRepository.GetByFiltersAsync(
            query.BarId, query.Estado, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = remitos
            .Select(r => new RemitoListItemResponse(
                r.Id,
                r.NumeroRemito,
                r.Fecha,
                r.BarId,
                r.Bar?.Nombre ?? string.Empty,
                r.Estado,
                r.Lineas.Sum(l => l.Subtotal)))
            .ToList();

        return Result.Success<IReadOnlyList<RemitoListItemResponse>>(response);
    }
}