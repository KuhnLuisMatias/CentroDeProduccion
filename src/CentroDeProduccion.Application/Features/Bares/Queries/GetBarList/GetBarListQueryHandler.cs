using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Bares.Queries;

namespace CentroDeProduccion.Application.Features.Bares.Queries.GetBarList;

public class GetBarListQueryHandler
{
    private readonly IBarRepository _barRepository;

    public GetBarListQueryHandler(IBarRepository barRepository)
    {
        _barRepository = barRepository;
    }

    public async Task<Result<IReadOnlyList<BarListItemResponse>>> HandleAsync(
        GetBarListQuery query, CancellationToken cancellationToken = default)
    {
        var bares = await _barRepository.GetByFiltersAsync(query.Estado, query.SearchTerm, cancellationToken);

        var response = bares.Select(b => new BarListItemResponse(
            b.Id,
            b.Nombre,
            b.Direccion,
            b.Encargado,
            b.Estado,
            b.MargenReventaPorcentaje)).ToList();

        return Result.Success<IReadOnlyList<BarListItemResponse>>(response);
    }
}