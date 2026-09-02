using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the insumo stock-below-minimum report. Only active insumos with StockActual ≤
/// StockMinimo are included; the difference is StockMinimo − StockActual.
/// </summary>
public class GetStockInsumosBajoMinimoReportQueryHandler
{
    private readonly IInsumoRepository _insumoRepository;

    public GetStockInsumosBajoMinimoReportQueryHandler(IInsumoRepository insumoRepository)
    {
        _insumoRepository = insumoRepository;
    }

    public async Task<Result<GetStockInsumosBajoMinimoReportDto>> HandleAsync(
        GetStockInsumosBajoMinimoReportQuery query, CancellationToken ct = default)
    {
        var insumos = await _insumoRepository.GetAllActiveAsync(ct);

        var items = insumos
            .Where(i => i.StockActual <= i.StockMinimo)
            .Select(i => new StockInsumoBajoMinimoReportItem(
                i.Id,
                i.Nombre,
                i.StockActual,
                i.StockMinimo,
                Math.Round(i.StockMinimo - i.StockActual, 2)))
            .OrderBy(i => i.DiferenciaStock)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            ReportType: "stock-insumos-bajo-minimo",
            ReportTitle: "Stock de insumos bajo mínimo");

        return Result.Success(new GetStockInsumosBajoMinimoReportDto(items, metadata));
    }
}
