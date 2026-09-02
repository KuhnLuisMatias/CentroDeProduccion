using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Produccion;

/// <summary>
/// Builds the production-by-recipe report for a date range, grouped by recipe. Recipe names come
/// from the loaded <see cref="Produccion.Receta"/> navigation.
/// </summary>
public class GetProduccionProductoReportQueryHandler
{
    private readonly IProduccionRepository _produccionRepository;

    public GetProduccionProductoReportQueryHandler(IProduccionRepository produccionRepository)
    {
        _produccionRepository = produccionRepository;
    }

    public async Task<Result<GetProduccionProductoReportDto>> HandleAsync(
        GetProduccionProductoReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetProduccionProductoReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var producciones = await _produccionRepository.GetByFiltersAsync(from, to, query.RecetaId, ct: ct);

        var items = producciones
            .GroupBy(p => new { p.RecetaId, RecetaNombre = p.Receta?.Nombre ?? string.Empty })
            .Select(g => new ProduccionProductoReportItem(
                g.Key.RecetaId,
                g.Key.RecetaNombre,
                g.Count(),
                g.Sum(p => p.CantidadProducida),
                g.Count() == 0 ? 0m : Math.Round(g.Average(p => p.CostoTotal), 2)))
            .OrderByDescending(i => i.CantidadProducida)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.RecetaId.HasValue ? $"Receta: {query.RecetaId.Value}" : null,
            "produccion-producto",
            "Producción por producto");

        return Result.Success(new GetProduccionProductoReportDto(items, metadata));
    }
}
