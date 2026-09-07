using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetPagosProveedorList;

public sealed record PagoAProveedorMetodoResponse(MetodoPago Tipo, decimal Monto, string? Referencia);

public sealed record PagoAProveedorResponse(
    Guid Id,
    Guid ProveedorId,
    string ProveedorNombre,
    DateTime Fecha,
    decimal MontoTotal,
    string? Observaciones,
    Guid? FacturaId,
    IReadOnlyList<PagoAProveedorMetodoResponse> Medios);

public sealed record GetPagosProveedorListQuery(
    Guid ProveedorId,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

public class GetPagosProveedorListQueryHandler
{
    private readonly IPagoAProveedorRepository _pagoAProveedorRepository;

    public GetPagosProveedorListQueryHandler(IPagoAProveedorRepository pagoAProveedorRepository)
    {
        _pagoAProveedorRepository = pagoAProveedorRepository;
    }

    public async Task<Result<IReadOnlyList<PagoAProveedorResponse>>> HandleAsync(
        GetPagosProveedorListQuery query, CancellationToken cancellationToken = default)
    {
        var pagos = await _pagoAProveedorRepository.GetByFiltersAsync(
            query.ProveedorId, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = pagos.Select(p => new PagoAProveedorResponse(
            p.Id,
            p.ProveedorId,
            p.Proveedor?.NombreRazonSocial ?? string.Empty,
            p.Fecha,
            p.MontoTotal,
            p.Observaciones,
            p.FacturaId,
            p.Medios.Select(m => new PagoAProveedorMetodoResponse(m.Tipo, m.Monto, m.Referencia)).ToList())).ToList();

        return Result.Success<IReadOnlyList<PagoAProveedorResponse>>(response);
    }
}
