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
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;

    public GetPagoListQueryHandler(
        IPagoProveedorRepository pagoProveedorRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository)
    {
        _pagoProveedorRepository = pagoProveedorRepository;
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
    }

    public async Task<Result<IReadOnlyList<PagoProveedorResponse>>> HandleAsync(
        GetPagoListQuery query, CancellationToken cancellationToken = default)
    {
        var pagos = await _pagoProveedorRepository.GetByFiltersAsync(
            query.ProveedorId, query.FechaDesde, query.FechaHasta, cancellationToken);

        // Batched derivation of settled amounts — one grouped query, no N+1.
        var facturaIds = pagos.Select(p => p.Id).ToList();
        var pagadosPorFactura = await _cuentaCorrienteRepository.GetPagosAplicadosPorFacturaAsync(
            facturaIds, cancellationToken);

        var response = pagos
            .Select(pago => GetPagoByIdQueryHandler.Map(pago, pagadosPorFactura.GetValueOrDefault(pago.Id)))
            .ToList();
        return Result.Success<IReadOnlyList<PagoProveedorResponse>>(response);
    }
}
