using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.PagosBar.Queries;

namespace CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarById;

public sealed record GetPagoBarByIdQuery(Guid Id);

public class GetPagoBarByIdQueryHandler
{
    private readonly IPagoBarRepository _pagoBarRepository;

    public GetPagoBarByIdQueryHandler(IPagoBarRepository pagoBarRepository)
    {
        _pagoBarRepository = pagoBarRepository;
    }

    public async Task<Result<PagoBarResponse>> HandleAsync(GetPagoBarByIdQuery query, CancellationToken cancellationToken = default)
    {
        var pagoBar = await _pagoBarRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (pagoBar == null)
        {
            return Result.Failure<PagoBarResponse>(Error.NotFound("PAGO_BAR_NOT_FOUND", "Pago de bar no encontrado"));
        }

        return Result.Success(Map(pagoBar));
    }

    internal static PagoBarResponse Map(Domain.Entities.PagoBar pagoBar) => new(
        pagoBar.Id,
        pagoBar.Numero,
        pagoBar.BarId,
        pagoBar.Bar?.Nombre ?? string.Empty,
        pagoBar.FechaPago,
        pagoBar.MontoTotal,
        pagoBar.Observaciones,
        pagoBar.Metodos.Select(m => new PagoBarMetodoResponse(m.Tipo, m.Monto, m.Referencia)).ToList(),
        pagoBar.Items.Select(i => new PagoBarItemResponse(
            i.RemitoId, i.Remito?.NumeroRemito ?? 0, i.MontoAplicado)).ToList());
}