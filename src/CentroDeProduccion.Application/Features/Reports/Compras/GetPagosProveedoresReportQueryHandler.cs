using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Builds the supplier-payments report for a date range: one row per PagoAProveedor with the
/// payment methods joined into a single string, plus the total paid per payment method.
/// Payment method names mirror the frontend METODO_PAGO_LABELS (no backend mapping existed).
/// </summary>
public class GetPagosProveedoresReportQueryHandler
{
    private static readonly Dictionary<MetodoPago, string> MedioNombres = new()
    {
        [MetodoPago.Efectivo] = "Efectivo",
        [MetodoPago.Transferencia] = "Transferencia",
        [MetodoPago.TarjetaDebito] = "Tarjeta de débito",
        [MetodoPago.TarjetaCredito] = "Tarjeta de crédito",
        [MetodoPago.Cheque] = "Cheque"
    };

    private readonly IPagoAProveedorRepository _pagoAProveedorRepository;
    private readonly IPagoProveedorRepository _pagoProveedorRepository;

    public GetPagosProveedoresReportQueryHandler(
        IPagoAProveedorRepository pagoAProveedorRepository,
        IPagoProveedorRepository pagoProveedorRepository)
    {
        _pagoAProveedorRepository = pagoAProveedorRepository;
        _pagoProveedorRepository = pagoProveedorRepository;
    }

    public async Task<Result<GetPagosProveedoresReportDto>> HandleAsync(
        GetPagosProveedoresReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetPagosProveedoresReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var pagos = await _pagoAProveedorRepository.GetByFiltersAsync(query.ProveedorId, from, to, ct);

        // Batched factura numbers for the "Pago factura N° X" reference (no N+1).
        var facturaIds = pagos.Where(p => p.FacturaId.HasValue).Select(p => p.FacturaId!.Value).Distinct().ToList();
        var facturasPorId = (await _pagoProveedorRepository.GetByIdsAsync(facturaIds, ct))
            .ToDictionary(f => f.Id);

        static string NombreMedio(MetodoPago tipo)
            => MedioNombres.TryGetValue(tipo, out var nombre) ? nombre : tipo.ToString();

        var items = pagos
            .Select(p =>
            {
                var factura = p.FacturaId.HasValue ? facturasPorId.GetValueOrDefault(p.FacturaId.Value) : null;
                var referencia = factura != null
                    ? $"Pago factura N° {factura.Numero}"
                    : (!string.IsNullOrWhiteSpace(p.Observaciones) ? p.Observaciones : "Pago a cuenta");
                return new PagosProveedoresReportItem(
                    p.Id,
                    p.Fecha,
                    p.ProveedorId,
                    p.Proveedor?.NombreRazonSocial ?? string.Empty,
                    p.MontoTotal,
                    string.Join("; ", p.Medios.Select(m => $"{NombreMedio(m.Tipo)} {m.Monto:N2}")),
                    referencia,
                    p.FacturaId,
                    p.Observaciones);
            })
            .ToList();

        var totalesPorMedio = pagos
            .SelectMany(p => p.Medios)
            .GroupBy(m => NombreMedio(m.Tipo))
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Monto));

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.ProveedorId.HasValue ? $"Proveedor: {query.ProveedorId.Value}" : null,
            "pagos-proveedores",
            "Pagos a proveedores");

        return Result.Success(new GetPagosProveedoresReportDto(items, totalesPorMedio, metadata));
    }
}
