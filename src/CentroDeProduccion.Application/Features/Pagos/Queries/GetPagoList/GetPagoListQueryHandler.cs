using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Pagos.Queries;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;

namespace CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoList;

public sealed record GetPagoListQuery(
    Guid? ProveedorId,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

public class GetPagoListQueryHandler
{
    private readonly IPagoProveedorRepository _pagoProveedorRepository;

    public GetPagoListQueryHandler(IPagoProveedorRepository pagoProveedorRepository)
    {
        _pagoProveedorRepository = pagoProveedorRepository;
    }

    public async Task<Result<IReadOnlyList<PagoProveedorResponse>>> HandleAsync(
        GetPagoListQuery query, CancellationToken cancellationToken = default)
    {
        var pagos = await _pagoProveedorRepository.GetByFiltersAsync(
            query.ProveedorId, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = pagos.Select(GetPagoByIdQueryHandler.Map).ToList();
        return Result.Success<IReadOnlyList<PagoProveedorResponse>>(response);
    }
}