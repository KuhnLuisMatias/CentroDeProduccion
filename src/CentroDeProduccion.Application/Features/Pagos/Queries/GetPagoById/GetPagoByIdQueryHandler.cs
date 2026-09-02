using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Pagos.Queries;

namespace CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;

public sealed record GetPagoByIdQuery(Guid Id);

public class GetPagoByIdQueryHandler
{
    private readonly IPagoProveedorRepository _pagoProveedorRepository;

    public GetPagoByIdQueryHandler(IPagoProveedorRepository pagoProveedorRepository)
    {
        _pagoProveedorRepository = pagoProveedorRepository;
    }

    public async Task<Result<PagoProveedorResponse>> HandleAsync(GetPagoByIdQuery query, CancellationToken cancellationToken = default)
    {
        var pago = await _pagoProveedorRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (pago == null)
        {
            return Result.Failure<PagoProveedorResponse>(Error.NotFound("PAGO_NOT_FOUND", "Factura no encontrada"));
        }

        return Result.Success(Map(pago));
    }

    internal static PagoProveedorResponse Map(Domain.Entities.PagoProveedor pago) => new(
        pago.Id,
        pago.Numero,
        pago.ProveedorId,
        pago.Proveedor?.NombreRazonSocial ?? string.Empty,
        pago.FechaPago,
        pago.MontoTotal,
        pago.Observaciones,
        pago.Metodos.Select(m => new PagoMetodoResponse(m.Tipo, m.Monto, m.Referencia)).ToList(),
        pago.Insumos.Select(i => new PagoInsumoResponse(
            i.InsumoId, i.Insumo?.Nombre ?? string.Empty, i.Cantidad, i.PrecioUnitario,
            i.Cantidad * i.PrecioUnitario)).ToList());
}