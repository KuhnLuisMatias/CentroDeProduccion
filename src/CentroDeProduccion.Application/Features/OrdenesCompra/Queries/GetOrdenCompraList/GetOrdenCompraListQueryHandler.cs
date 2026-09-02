using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraById;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraList;

public class GetOrdenCompraListQueryHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;

    public GetOrdenCompraListQueryHandler(IOrdenCompraRepository ordenCompraRepository)
    {
        _ordenCompraRepository = ordenCompraRepository;
    }

    public async Task<Result<IReadOnlyList<OrdenCompraResponse>>> HandleAsync(
        GetOrdenCompraListQuery query, CancellationToken cancellationToken = default)
    {
        var ordenes = await _ordenCompraRepository.GetByFiltersAsync(
            query.ProveedorId, query.Estado, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = ordenes.Select(GetOrdenCompraByIdQueryHandler.Map).ToList();
        return Result.Success<IReadOnlyList<OrdenCompraResponse>>(response);
    }
}