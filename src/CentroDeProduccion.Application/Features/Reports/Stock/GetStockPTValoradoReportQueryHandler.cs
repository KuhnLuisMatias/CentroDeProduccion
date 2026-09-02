using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the valued finished-product stock report for all active finished products. Each value
/// is StockActual × cost, where cost is computed on the fly from the product's recipe BOM at
/// current insumo prices (no stored cost).
/// </summary>
public class GetStockPTValoradoReportQueryHandler
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;

    public GetStockPTValoradoReportQueryHandler(
        IProductoTerminadoRepository productoTerminadoRepository,
        ProductoTerminadoCostoResolver costoResolver)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
        _costoResolver = costoResolver;
    }

    public async Task<Result<GetStockPTValoradoReportDto>> HandleAsync(
        GetStockPTValoradoReportQuery query, CancellationToken ct = default)
    {
        var productos = await _productoTerminadoRepository.GetAllActiveAsync(ct);
        var costos = await _costoResolver.CalcularPorRecetasAsync(
            productos.Select(p => p.RecetaId), ct);

        var items = productos
            .Select(p =>
            {
                var costo = costos.GetValueOrDefault(p.RecetaId ?? Guid.Empty);
                return new StockPTValoradoReportItem(
                    p.Id,
                    p.Nombre,
                    p.StockActual,
                    costo,
                    Math.Round(p.StockActual * costo, 2));
            })
            .OrderByDescending(i => i.ValorTotal)
            .ToList();

        var totalValorizado = Math.Round(items.Sum(i => i.ValorTotal), 2);

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            ReportType: "stock-pt-valorado",
            ReportTitle: "Stock de productos terminados valorizado");

        return Result.Success(new GetStockPTValoradoReportDto(items, totalValorizado, metadata));
    }
}
