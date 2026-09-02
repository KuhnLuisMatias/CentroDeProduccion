using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the sales-by-period report from delivered (Enviado) delivery notes, grouped by day, week
/// or month.
/// </summary>
public class GetVentasPeriodoReportQueryHandler
{
    private const string ErrorCode = "AGRUPACION_INVALIDA";

    private readonly IRemitoRepository _remitoRepository;

    public GetVentasPeriodoReportQueryHandler(IRemitoRepository remitoRepository)
    {
        _remitoRepository = remitoRepository;
    }

    public async Task<Result<GetVentasPeriodoReportDto>> HandleAsync(
        GetVentasPeriodoReportQuery query, CancellationToken ct = default)
    {
        var agrupacion = query.Agrupacion?.Trim().ToLowerInvariant();
        if (agrupacion is not ("dia" or "semana" or "mes"))
        {
            return Result.Failure<GetVentasPeriodoReportDto>(
                Error.Validation(ErrorCode, $"Agrupación inválida: '{query.Agrupacion}'. Use 'dia', 'semana' o 'mes'."));
        }

        var today = DateTime.Today;
        var from = query.From ?? new DateTime(today.Year, today.Month, 1);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetVentasPeriodoReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(null, EstadoRemito.Enviado, from, to, ct);

        var grouped = agrupacion switch
        {
            "semana" => remitos
                .GroupBy(r => StartOfWeek(r.Fecha))
                .Select(g => new VentasPeriodoReportItem(
                    g.Key.ToString("dd/MM/yyyy"),
                    g.Count(),
                    g.Sum(r => r.Lineas.Sum(l => l.Cantidad)),
                    Math.Round(g.Sum(r => r.Lineas.Sum(l => l.Subtotal)), 2))),
            "mes" => remitos
                .GroupBy(r => new DateTime(r.Fecha.Year, r.Fecha.Month, 1))
                .Select(g => new VentasPeriodoReportItem(
                    g.Key.ToString("MMM yyyy"),
                    g.Count(),
                    g.Sum(r => r.Lineas.Sum(l => l.Cantidad)),
                    Math.Round(g.Sum(r => r.Lineas.Sum(l => l.Subtotal)), 2))),
            _ => remitos
                .GroupBy(r => r.Fecha.Date)
                .Select(g => new VentasPeriodoReportItem(
                    g.Key.ToString("dd/MM/yyyy"),
                    g.Count(),
                    g.Sum(r => r.Lineas.Sum(l => l.Cantidad)),
                    Math.Round(g.Sum(r => r.Lineas.Sum(l => l.Subtotal)), 2)))
        };

        var items = grouped
            .OrderBy(i => i.PeriodoLabel)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            $"Agrupación: {agrupacion}",
            "ventas-periodo",
            "Ventas por período");

        return Result.Success(new GetVentasPeriodoReportDto(items, metadata));
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.Date.AddDays(-diff);
    }
}
