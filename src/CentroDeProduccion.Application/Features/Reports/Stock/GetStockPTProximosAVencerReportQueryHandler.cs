using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Stock;

/// <summary>
/// Builds the products-nearing-expiration report. Finished products expiring between today and
/// the next 7 days, sorted ascending by expiration date. <see cref="ProductoTerminadoRepository"/>
/// returns candidates with FechaVencimiento ≤ hasta; the lower bound is filtered here.
/// </summary>
public class GetStockPTProximosAVencerReportQueryHandler
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;

    public GetStockPTProximosAVencerReportQueryHandler(IProductoTerminadoRepository productoTerminadoRepository)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
    }

    public async Task<Result<GetStockPTProximosAVencerReportDto>> HandleAsync(
        GetStockPTProximosAVencerReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var horizon = today.AddDays(7);

        var candidatos = await _productoTerminadoRepository.GetProximosAVencerAsync(horizon, ct);

        var items = candidatos
            .Where(p => p.FechaVencimiento >= today)
            .Select(p => new StockPTProximoAVencerReportItem(
                p.Id,
                p.Nombre,
                p.StockActual,
                p.FechaVencimiento,
                (p.FechaVencimiento.Date - today).Days))
            .OrderBy(i => i.FechaVencimiento)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            today,
            horizon,
            "Próximos a vencer (7 días)",
            "stock-pt-proximos-a-vencer",
            "Productos terminados próximos a vencer");

        return Result.Success(new GetStockPTProximosAVencerReportDto(items, metadata));
    }
}
