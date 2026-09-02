using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Builds the supplier current-account report. The opening balance is derived from the movements
/// before the range start, then the running balance is computed per in-range movement.
/// </summary>
public class GetCtaCteProveedorReportQueryHandler
{
    private readonly ICuentaCorrienteProveedorRepository _ctaCteRepository;

    public GetCtaCteProveedorReportQueryHandler(ICuentaCorrienteProveedorRepository ctaCteRepository)
    {
        _ctaCteRepository = ctaCteRepository;
    }

    public async Task<Result<GetCtaCteProveedorReportDto>> HandleAsync(
        GetCtaCteProveedorReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-90);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetCtaCteProveedorReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var movimientos = await _ctaCteRepository.GetByProveedorAsync(query.ProveedorId, null, from, to, ct);
        var movimientosAntes = await _ctaCteRepository.GetByProveedorAsync(query.ProveedorId, null, null, from.AddTicks(-1), ct);

        var saldo = movimientosAntes.Sum(m => m.Monto);
        var saldoFinal = saldo;

        var items = new List<CtaCteProveedorReportItem>(movimientos.Count);
        foreach (var m in movimientos)
        {
            saldo += m.Monto;
            items.Add(new CtaCteProveedorReportItem(
                m.Fecha,
                m.TipoMovimiento.ToString(),
                m.Referencia,
                m.Monto,
                Math.Round(saldo, 2)));
        }

        saldoFinal = Math.Round(saldo, 2);

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            $"Proveedor: {query.ProveedorId}",
            "cta-cte-proveedor",
            "Cuenta corriente del proveedor");

        return Result.Success(new GetCtaCteProveedorReportDto(items, metadata, saldoFinal));
    }
}
