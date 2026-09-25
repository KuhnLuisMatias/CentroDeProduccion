using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Pagos.Queries;

namespace CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;

public sealed record GetPagoByIdQuery(Guid Id);

public class GetPagoByIdQueryHandler
{
    private readonly IPagoProveedorRepository _pagoProveedorRepository;
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IPagoAProveedorRepository _pagoAProveedorRepository;

    public GetPagoByIdQueryHandler(
        IPagoProveedorRepository pagoProveedorRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IPagoAProveedorRepository pagoAProveedorRepository)
    {
        _pagoProveedorRepository = pagoProveedorRepository;
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _pagoAProveedorRepository = pagoAProveedorRepository;
    }

    public async Task<Result<PagoProveedorResponse>> HandleAsync(GetPagoByIdQuery query, CancellationToken cancellationToken = default)
    {
        var pago = await _pagoProveedorRepository.GetByIdWithDetailsAsync(query.Id, cancellationToken);
        if (pago == null)
        {
            return Result.Failure<PagoProveedorResponse>(Error.NotFound("PAGO_NOT_FOUND", "Factura no encontrada"));
        }

        var pagadosPorFactura = await _cuentaCorrienteRepository.GetPagosAplicadosPorFacturaAsync(
            [pago.Id], cancellationToken);

        var pagosRealizados = await _pagoAProveedorRepository.GetByFacturaIdAsync(pago.Id, cancellationToken);

        return Result.Success(Map(pago, pagadosPorFactura.GetValueOrDefault(pago.Id), pagosRealizados));
    }

    public static PagoProveedorResponse Map(
        Domain.Entities.PagoProveedor pago,
        decimal montoPagado,
        IReadOnlyList<Domain.Entities.PagoAProveedor>? pagosRealizados = null)
    {
        var montoPendiente = Math.Max(0m, pago.MontoTotal - montoPagado);
        var estadoPago =
            montoPendiente <= 0m && pago.MontoTotal > 0m ? "Pagada" :
            montoPagado > 0m ? "Parcial" : "Pendiente";

        return new(
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
                i.Cantidad * i.PrecioUnitario)).ToList(),
            montoPagado,
            montoPendiente,
            estadoPago,
            pagosRealizados?.Select(p => new PagoRealizadoResponse(
                p.Id,
                p.Fecha,
                p.MontoTotal,
                p.Observaciones,
                p.Medios.Select(m => new PagoRealizadoMedioResponse(
                    m.Tipo, m.Monto, m.Referencia,
                    m.ChequeNumero, m.ChequeBanco, m.ChequeFechaPago)).ToList())).ToList());
    }
}