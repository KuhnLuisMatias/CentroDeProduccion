using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the insumo stock-movements report. Only movements with a non-null
/// <see cref="MovimientoStock.InsumoId"/> are included; names come from the loaded navigation.
/// </summary>
public class GetStockInsumosMovimientosReportQueryHandler
{
    private readonly IMovimientoStockRepository _movimientoStockRepository;

    public GetStockInsumosMovimientosReportQueryHandler(IMovimientoStockRepository movimientoStockRepository)
    {
        _movimientoStockRepository = movimientoStockRepository;
    }

    public async Task<Result<GetStockInsumosMovimientosReportDto>> HandleAsync(
        GetStockInsumosMovimientosReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetStockInsumosMovimientosReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var movimientos = await _movimientoStockRepository.GetByFiltersAsync(
            from, to, tipo: query.Tipo, ct: ct);

        var items = movimientos
            .Where(m => m.InsumoId.HasValue)
            .Select(m =>
            {
                var costoUnitario = m.PrecioUnitario ?? 0m;
                return new StockInsumoMovimientoReportItem(
                    m.Fecha,
                    m.InsumoId!.Value,
                    m.Insumo?.Nombre ?? string.Empty,
                    m.Tipo,
                    m.Cantidad,
                    costoUnitario,
                    Math.Round(m.Cantidad * costoUnitario, 2));
            })
            .OrderByDescending(i => i.Fecha)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.Tipo.HasValue ? $"Tipo: {query.Tipo.Value}" : null,
            "stock-insumos-movimientos",
            "Movimientos de stock de insumos");

        return Result.Success(new GetStockInsumosMovimientosReportDto(items, metadata));
    }
}
