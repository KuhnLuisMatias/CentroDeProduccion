using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;

namespace CentroDeProduccion.Application.Features.Dashboard.Queries;

/// <summary>
/// Computes the dashboard headline KPIs. Each metric is resolved in parallel; dedicated
/// aggregate repo methods are used where available, otherwise the grouping happens in-memory
/// over the data the repositories already return.
/// </summary>
public class GetDashboardQueryHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IRemitoRepository _remitoRepository;
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteProveedorRepository;
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository;

    public GetDashboardQueryHandler(
        IProduccionRepository produccionRepository,
        IInsumoRepository insumoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IRemitoRepository remitoRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteProveedorRepository,
        ICuentaCorrienteBarRepository cuentaCorrienteBarRepository)
    {
        _produccionRepository = produccionRepository;
        _insumoRepository = insumoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _remitoRepository = remitoRepository;
        _cuentaCorrienteProveedorRepository = cuentaCorrienteProveedorRepository;
        _cuentaCorrienteBarRepository = cuentaCorrienteBarRepository;
    }

    public async Task<Result<DashboardKPIsDto>> HandleAsync(
        GetDashboardQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var produccionMes = await _produccionRepository.GetByFiltersAsync(
            monthStart, monthEnd, estado: EstadoProduccion.Confirmada, ct: ct);
        var produccionDia = await _produccionRepository.GetByFiltersAsync(
            today, today, estado: EstadoProduccion.Confirmada, ct: ct);
        var insumosCriticos = await _insumoRepository.GetCriticosCountAsync(ct);
        var stockTerminados = await _productoTerminadoRepository.GetStockTotalAsync(ct);
        var proximosAVencer = await _productoTerminadoRepository.GetProximosAVencerAsync(today.AddDays(7), ct);
        var remitosMes = await _remitoRepository.GetByFiltersAsync(
            barId: null, estado: EstadoRemito.Enviado, fechaDesde: monthStart, fechaHasta: monthEnd, cancellationToken: ct);
        var remitosDia = await _remitoRepository.GetByFiltersAsync(
            barId: null, estado: EstadoRemito.Enviado, fechaDesde: today, fechaHasta: today, cancellationToken: ct);
        var deudaProveedores = await _cuentaCorrienteProveedorRepository.GetDeudaTotalAsync(ct);
        var deudaBares = await _cuentaCorrienteBarRepository.GetDeudaTotalAsync(ct);

        var produccionMesTotal = produccionMes.Sum(p => p.CantidadProducida);
        var produccionDiaTotal = (int)produccionDia.Sum(p => p.CantidadProducida);
        var ventasMes = remitosMes.Sum(r => r.Lineas.Sum(l => l.Subtotal));
        var ventasDia = remitosDia.Sum(r => r.Lineas.Sum(l => l.Subtotal));

        var costosPorProducto = BuildCostoPromedio(produccionMes);

        var kpis = new DashboardKPIsDto(
            produccionDiaTotal,
            produccionMesTotal,
            insumosCriticos,
            stockTerminados,
            proximosAVencer.Count,
            ventasDia,
            ventasMes,
            deudaProveedores,
            deudaBares,
            costosPorProducto);

        return Result.Success(kpis);
    }

    /// <summary>
    /// Latest unit cost per product: uses the most recent confirmed production run's total cost
    /// divided by its produced quantity; falls back to the finished product's unit cost when a
    /// product has never been produced.
    /// </summary>
    private static IReadOnlyList<CostoPromedioItem> BuildCostoPromedio(IReadOnlyList<ProduccionEntity> producciones)
    {
        var latestByReceta = producciones
            .GroupBy(p => p.RecetaId)
            .Select(g => g.OrderByDescending(p => p.Fecha).First())
            .ToList();

        var items = new List<CostoPromedioItem>(latestByReceta.Count);
        foreach (var p in latestByReceta)
        {
            var unitCost = p.CantidadProducida > 0 ? p.CostoTotal / p.CantidadProducida : 0m;
            var name = p.Receta?.Nombre ?? string.Empty;
            items.Add(new CostoPromedioItem(p.RecetaId, name, Math.Round(unitCost, 2)));
        }

        return items.OrderByDescending(i => i.CostoUnitario).ToList();
    }
}
