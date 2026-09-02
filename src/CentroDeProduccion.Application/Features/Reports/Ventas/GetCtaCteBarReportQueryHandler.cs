using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the bar current-account report. The opening balance is derived from the movements before
/// the range start, then the running balance is computed per in-range movement.
/// </summary>
public class GetCtaCteBarReportQueryHandler
{
    private readonly ICuentaCorrienteBarRepository _ctaCteRepository;

    public GetCtaCteBarReportQueryHandler(ICuentaCorrienteBarRepository ctaCteRepository)
    {
        _ctaCteRepository = ctaCteRepository;
    }

    public async Task<Result<GetCtaCteBarReportDto>> HandleAsync(
        GetCtaCteBarReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-90);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetCtaCteBarReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var movimientos = await _ctaCteRepository.GetByBarAsync(query.BarId, null, from, to, ct);
        var movimientosAntes = await _ctaCteRepository.GetByBarAsync(query.BarId, null, null, from.AddTicks(-1), ct);

        var saldo = movimientosAntes.Sum(m => m.Monto);

        var items = new List<CtaCteBarReportItem>(movimientos.Count);
        foreach (var m in movimientos)
        {
            saldo += m.Monto;
            items.Add(new CtaCteBarReportItem(
                m.Fecha,
                m.TipoMovimiento.ToString(),
                m.Referencia,
                m.Monto,
                Math.Round(saldo, 2)));
        }

        var saldoFinal = Math.Round(saldo, 2);

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            $"Bar: {query.BarId}",
            "cta-cte-bar",
            "Cuenta corriente del bar");

        return Result.Success(new GetCtaCteBarReportDto(items, metadata, saldoFinal));
    }
}
