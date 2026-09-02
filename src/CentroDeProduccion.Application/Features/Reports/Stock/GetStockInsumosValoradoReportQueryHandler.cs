using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the valued insumo stock report for all active insumos. Each value is
/// StockActual × PrecioUltimaCompra; the total sums them.
/// </summary>
public class GetStockInsumosValoradoReportQueryHandler
{
    private readonly IInsumoRepository _insumoRepository;

    public GetStockInsumosValoradoReportQueryHandler(IInsumoRepository insumoRepository)
    {
        _insumoRepository = insumoRepository;
    }

    public async Task<Result<GetStockInsumosValoradoReportDto>> HandleAsync(
        GetStockInsumosValoradoReportQuery query, CancellationToken ct = default)
    {
        var insumos = await _insumoRepository.GetAllActiveAsync(ct);

        var items = insumos
            .Select(i => new StockInsumoValoradoReportItem(
                i.Id,
                i.Nombre,
                i.UnidadConsumo?.Nombre ?? i.UnidadCompra?.Nombre ?? string.Empty,
                i.StockActual,
                i.PrecioUltimaCompra,
                Math.Round(i.StockActual * i.PrecioUltimaCompra, 2)))
            .OrderByDescending(i => i.ValorTotal)
            .ToList();

        var totalValorizado = Math.Round(items.Sum(i => i.ValorTotal), 2);

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            ReportType: "stock-insumos-valorado",
            ReportTitle: "Stock de insumos valorizado");

        return Result.Success(new GetStockInsumosValoradoReportDto(items, totalValorizado, metadata));
    }
}
