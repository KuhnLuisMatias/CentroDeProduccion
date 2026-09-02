using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Builds the production-by-period report for a date range, grouped by day, week or month.
/// </summary>
public class GetProduccionPeriodoReportQueryHandler
{
    private const string ErrorCode = "AGRUPACION_INVALIDA";

    private readonly IProduccionRepository _produccionRepository;

    public GetProduccionPeriodoReportQueryHandler(IProduccionRepository produccionRepository)
    {
        _produccionRepository = produccionRepository;
    }

    public async Task<Result<GetProduccionPeriodoReportDto>> HandleAsync(
        GetProduccionPeriodoReportQuery query, CancellationToken ct = default)
    {
        var agrupacion = query.Agrupacion?.Trim().ToLowerInvariant();
        if (agrupacion is not ("dia" or "semana" or "mes"))
        {
            return Result.Failure<GetProduccionPeriodoReportDto>(
                Error.Validation(ErrorCode, $"Agrupación inválida: '{query.Agrupacion}'. Use 'dia', 'semana' o 'mes'."));
        }

        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetProduccionPeriodoReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var producciones = await _produccionRepository.GetByDateRangeAsync(from, to, ct);

        var grouped = agrupacion switch
        {
            "semana" => producciones
                .GroupBy(p => StartOfWeek(p.Fecha))
                .Select(g => new ProduccionPeriodoReportItem(
                    g.Key.ToString("dd/MM/yyyy"),
                    g.Count(),
                    g.Sum(p => p.CantidadProducida),
                    g.Sum(p => p.CostoTotal))),
            "mes" => producciones
                .GroupBy(p => new DateTime(p.Fecha.Year, p.Fecha.Month, 1))
                .Select(g => new ProduccionPeriodoReportItem(
                    g.Key.ToString("MMM yyyy"),
                    g.Count(),
                    g.Sum(p => p.CantidadProducida),
                    g.Sum(p => p.CostoTotal))),
            _ => producciones
                .GroupBy(p => p.Fecha.Date)
                .Select(g => new ProduccionPeriodoReportItem(
                    g.Key.ToString("dd/MM/yyyy"),
                    g.Count(),
                    g.Sum(p => p.CantidadProducida),
                    g.Sum(p => p.CostoTotal)))
        };

        var items = grouped
            .OrderBy(i => i.PeriodoLabel)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            $"Agrupación: {agrupacion}",
            "produccion-periodo",
            "Producción por período");

        return Result.Success(new GetProduccionPeriodoReportDto(items, metadata));
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.Date.AddDays(-diff);
    }
}
