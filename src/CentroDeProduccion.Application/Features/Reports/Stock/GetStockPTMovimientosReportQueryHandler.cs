using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the finished-product stock-movements report. The repository loads <see cref="MovimientoStock.Insumo"/>
/// but not the finished product, so names are resolved via <see cref="IProductoTerminadoRepository.GetByIdsAsync"/>.
/// </summary>
public class GetStockPTMovimientosReportQueryHandler
{
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;

    public GetStockPTMovimientosReportQueryHandler(
        IMovimientoStockRepository movimientoStockRepository,
        IProductoTerminadoRepository productoTerminadoRepository)
    {
        _movimientoStockRepository = movimientoStockRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
    }

    public async Task<Result<GetStockPTMovimientosReportDto>> HandleAsync(
        GetStockPTMovimientosReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetStockPTMovimientosReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var movimientos = await _movimientoStockRepository.GetByFiltersAsync(
            from, to, productoTerminadoId: query.ProductoTerminadoId, ct: ct);

        var ptIds = movimientos
            .Where(m => m.ProductoTerminadoId.HasValue)
            .Select(m => m.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();

        var ptLookup = (await _productoTerminadoRepository.GetByIdsAsync(ptIds, ct))
            .ToDictionary(p => p.Id, p => p);

        var items = movimientos
            .Where(m => m.ProductoTerminadoId.HasValue)
            .Select(m =>
            {
                var costoUnitario = m.PrecioUnitario ?? 0m;
                var pt = ptLookup.TryGetValue(m.ProductoTerminadoId!.Value, out var p) ? p : null;
                return new StockPTMovimientoReportItem(
                    m.Fecha,
                    m.ProductoTerminadoId!.Value,
                    pt?.Nombre ?? string.Empty,
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
            query.ProductoTerminadoId.HasValue ? $"Producto: {query.ProductoTerminadoId.Value}" : null,
            "stock-pt-movimientos",
            "Movimientos de stock de productos terminados");

        return Result.Success(new GetStockPTMovimientosReportDto(items, metadata));
    }
}
