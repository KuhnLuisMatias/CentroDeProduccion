using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.PagosBar.Queries;

namespace CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarList;

public sealed record GetPagoBarListQuery(
    Guid? BarId,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

public class GetPagoBarListQueryHandler
{
    private readonly IPagoBarRepository _pagoBarRepository;

    public GetPagoBarListQueryHandler(IPagoBarRepository pagoBarRepository)
    {
        _pagoBarRepository = pagoBarRepository;
    }

    public async Task<Result<IReadOnlyList<PagoBarListResponse>>> HandleAsync(
        GetPagoBarListQuery query, CancellationToken cancellationToken = default)
    {
        var pagos = await _pagoBarRepository.GetByFiltersAsync(
            query.BarId, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = pagos.Select(p => new PagoBarListResponse(
            p.Id, p.Numero, p.BarId, p.Bar?.Nombre ?? string.Empty, p.FechaPago, p.MontoTotal, p.Metodos.Count)).ToList();
        return Result.Success<IReadOnlyList<PagoBarListResponse>>(response);
    }
}