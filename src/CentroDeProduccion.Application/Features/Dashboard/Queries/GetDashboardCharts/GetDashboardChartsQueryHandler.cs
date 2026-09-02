using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;

namespace CentroDeProduccion.Application.Features.Dashboard.Queries;

/// <summary>
/// Builds the dashboard charts. Each chart groups data retrieved from the repositories; empty
/// data yields a chart with empty labels/datasets (the UI renders an empty state).
/// </summary>
public class GetDashboardChartsQueryHandler
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IRemitoRepository _remitoRepository;
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteProveedorRepository;
    private readonly IProveedorRepository _proveedorRepository;

    public GetDashboardChartsQueryHandler(
        IProduccionRepository produccionRepository,
        IInsumoRepository insumoRepository,
        IRemitoRepository remitoRepository,
        IOrdenCompraRepository ordenCompraRepository,
        ICuentaCorrienteProveedorRepository cuentaCorrienteProveedorRepository,
        IProveedorRepository proveedorRepository)
    {
        _produccionRepository = produccionRepository;
        _insumoRepository = insumoRepository;
        _remitoRepository = remitoRepository;
        _ordenCompraRepository = ordenCompraRepository;
        _cuentaCorrienteProveedorRepository = cuentaCorrienteProveedorRepository;
        _proveedorRepository = proveedorRepository;
    }

    public async Task<Result<DashboardChartsDto>> HandleAsync(
        GetDashboardChartsQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var yearStart = new DateTime(today.Year, 1, 1);

        var produccionMes = await _produccionRepository.GetByFiltersAsync(
            monthStart, monthEnd, estado: EstadoProduccion.Confirmada, ct: ct);
        var produccion12 = await _produccionRepository.GetByDateRangeAsync(
            yearStart, today, ct);
        var insumos = await _insumoRepository.GetAllActiveAsync(ct);
        var remitosMes = await _remitoRepository.GetByFiltersAsync(
            barId: null, estado: EstadoRemito.Enviado, fechaDesde: monthStart, fechaHasta: monthEnd, cancellationToken: ct);
        var ordenesMes = await _ordenCompraRepository.GetByFiltersAsync(
            proveedorId: null, estado: EstadoOrdenCompra.Enviada, fechaDesde: monthStart, fechaHasta: monthEnd, cancellationToken: ct);
        var proveedores = await _proveedorRepository.GetAllActiveAsync(ct);

        var charts = new List<ChartDto>
        {
            ProduccionDiariaMes(produccionMes, monthStart),
            Top5Productos(produccionMes),
            StockInsumosPorNivel(insumos),
            EvolucionCostos12Meses(produccion12),
            RemitosPorBar(remitosMes),
            ComprasPorProveedor(ordenesMes),
            await EstadoCuentaProveedores(proveedores, ct)
        };

        return Result.Success(new DashboardChartsDto(charts));
    }

    // 1. Production per day of the current month (bar).
    private static ChartDto ProduccionDiariaMes(IReadOnlyList<ProduccionEntity> producciones, DateTime monthStart)
    {
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        var labels = Enumerable.Range(1, daysInMonth).Select(d => d.ToString()).ToList();
        var data = new decimal[daysInMonth];
        foreach (var p in producciones)
        {
            var idx = p.Fecha.Day - 1;
            if (idx >= 0 && idx < daysInMonth)
                data[idx] += p.CantidadProducida;
        }

        return new ChartDto(
            "bar",
            "Producción diaria del mes",
            labels,
            new[] { new ChartDataset("Unidades producidas", data) });
    }

    // 2. Top 5 recipes by produced quantity this month (pie).
    private static ChartDto Top5Productos(IReadOnlyList<ProduccionEntity> producciones)
    {
        var top = producciones
            .GroupBy(p => new { p.RecetaId, Nombre = p.Receta?.Nombre ?? string.Empty })
            .Select(g => new { g.Key.Nombre, Total = g.Sum(p => p.CantidadProducida) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToList();

        return new ChartDto(
            "pie",
            "Top 5 productos por producción del mes",
            top.Select(x => x.Nombre).ToList(),
            new[] { new ChartDataset("Unidades", top.Select(x => x.Total).ToList()) });
    }

    // 3. Insumo stock by level: Critico (<= stockMinimo), Bajo (<= 1.5x), Normal.
    private static ChartDto StockInsumosPorNivel(IReadOnlyList<Insumo> insumos)
    {
        var critico = 0;
        var bajo = 0;
        var normal = 0;

        foreach (var i in insumos)
        {
            if (i.StockActual <= i.StockMinimo)
                critico++;
            else if (i.StockActual <= i.StockMinimo * 1.5m)
                bajo++;
            else
                normal++;
        }

        return new ChartDto(
            "horizontalBar",
            "Stock de insumos por nivel",
            new[] { "Crítico", "Bajo", "Normal" },
            new[]
            {
                new ChartDataset("Cantidad de insumos", new decimal[] { critico, bajo, normal },
                    BackgroundColor: null)
            });
    }

    // 4. Average production cost per month over the last 12 months (line).
    private static ChartDto EvolucionCostos12Meses(IReadOnlyList<ProduccionEntity> producciones)
    {
        var from = DateTime.Today.AddMonths(-11);
        var start = new DateTime(from.Year, from.Month, 1);

        var labels = new List<string>();
        var data = new List<decimal>();
        for (var month = start; month <= DateTime.Today; month = month.AddMonths(1))
        {
            var inMonth = producciones
                .Where(p => p.Fecha >= month && p.Fecha < month.AddMonths(1))
                .ToList();
            labels.Add(month.ToString("MMM yyyy"));
            data.Add(inMonth.Count == 0 ? 0m : Math.Round(inMonth.Average(p => p.CostoTotal), 2));
        }

        return new ChartDto(
            "line",
            "Evolución del costo de producción (12 meses)",
            labels,
            new[] { new ChartDataset("Costo promedio", data) });
    }

    // 5. Count of sent remitos grouped by bar this month (bar).
    private static ChartDto RemitosPorBar(IReadOnlyList<Remito> remitos)
    {
        var byBar = remitos
            .GroupBy(r => r.Bar?.Nombre ?? string.Empty)
            .Select(g => new { Nombre = g.Key, Total = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        return new ChartDto(
            "bar",
            "Remitos enviados por bar (mes)",
            byBar.Select(x => x.Nombre).ToList(),
            new[] { new ChartDataset("Remitos", byBar.Select(x => (decimal)x.Total).ToList()) });
    }

    // 6. Sent purchase-order amounts grouped by proveedor this month (pie).
    private static ChartDto ComprasPorProveedor(IReadOnlyList<OrdenCompra> ordenes)
    {
        var byProveedor = ordenes
            .GroupBy(o => o.Proveedor?.NombreRazonSocial ?? string.Empty)
            .Select(g => new
            {
                Nombre = g.Key,
                Total = g.Sum(o => o.Items.Sum(i => i.CantidadPedida * i.PrecioUnitario))
            })
            .Where(x => x.Total != 0)
            .OrderByDescending(x => x.Total)
            .ToList();

        return new ChartDto(
            "pie",
            "Compras por proveedor (mes)",
            byProveedor.Select(x => x.Nombre).ToList(),
            new[] { new ChartDataset("Monto", byProveedor.Select(x => x.Total).ToList()) });
    }

    // 7. Current saldo per proveedor (bar).
    private async Task<ChartDto> EstadoCuentaProveedores(
        IReadOnlyList<Proveedor> proveedores, CancellationToken ct)
    {
        var saldos = await _cuentaCorrienteProveedorRepository.GetSaldosPorProveedorAsync(ct);
        var labels = new List<string>();
        var data = new List<decimal>();
        foreach (var proveedor in proveedores)
        {
            saldos.TryGetValue(proveedor.Id, out var saldo);
            labels.Add(proveedor.NombreRazonSocial);
            data.Add(saldo);
        }

        return new ChartDto(
            "bar",
            "Estado de cuenta por proveedor",
            labels,
            new[] { new ChartDataset("Saldo", data) });
    }
}
