using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the sales-by-bar report from delivered (Enviado) delivery notes. Bar names come from the
/// loaded <see cref="Remito.Bar"/> navigation; subtotals from the loaded <see cref="Remito.Lineas"/>.
/// </summary>
public class GetVentasPorBarReportQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;

    public GetVentasPorBarReportQueryHandler(IRemitoRepository remitoRepository)
    {
        _remitoRepository = remitoRepository;
    }

    public async Task<Result<GetVentasPorBarReportDto>> HandleAsync(
        GetVentasPorBarReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? new DateTime(today.Year, today.Month, 1);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetVentasPorBarReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(query.BarId, EstadoRemito.Enviado, from, to, ct);

        var items = remitos
            .GroupBy(r => new { r.BarId, BarNombre = r.Bar?.Nombre ?? string.Empty })
            .Select(g => new VentasPorBarReportItem(
                g.Key.BarId,
                g.Key.BarNombre,
                g.Count(),
                g.Sum(r => r.Lineas.Count),
                Math.Round(g.Sum(r => r.Lineas.Sum(l => l.Subtotal)), 2)))
            .OrderByDescending(i => i.TotalSubtotal)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.BarId.HasValue ? $"Bar: {query.BarId.Value}" : null,
            "ventas-por-bar",
            "Ventas por bar");

        return Result.Success(new GetVentasPorBarReportDto(items, metadata));
    }
}
