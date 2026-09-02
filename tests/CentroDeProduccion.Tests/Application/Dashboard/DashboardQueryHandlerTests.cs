using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Dashboard.Queries;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;

namespace CentroDeProduccion.Tests.Application.Dashboard;

/// <summary>
/// Verifies the dashboard headline KPIs. Each metric is resolved from a dedicated aggregate
/// repository call; empty data must yield zero/empty KPIs without throwing.
/// </summary>
public class GetDashboardQueryHandlerTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteProveedorRepository = Substitute.For<ICuentaCorrienteProveedorRepository>();
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository = Substitute.For<ICuentaCorrienteBarRepository>();

    private GetDashboardQueryHandler CreateHandler() => new(
        _produccionRepository, _insumoRepository, _productoTerminadoRepository, _remitoRepository,
        _cuentaCorrienteProveedorRepository, _cuentaCorrienteBarRepository);

    private void ConfigurarProduccionMes(IReadOnlyList<ProduccionEntity> mes)
    {
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _produccionRepository.GetByFiltersAsync(
            Arg.Is<DateTime>(d => d == monthStart), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(mes);
    }

    private void ConfigurarProduccionDia(IReadOnlyList<ProduccionEntity> dia)
    {
        var today = DateTime.Today;
        _produccionRepository.GetByFiltersAsync(
            Arg.Is<DateTime>(d => d == today), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(dia);
    }

    private void ConfigurarRemitosMes(IReadOnlyList<Remito> remitos)
    {
        _remitoRepository.GetByFiltersAsync(
            Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(remitos);
    }

    private void ConfigurarRemitosDia(IReadOnlyList<Remito> remitos)
    {
        var today = DateTime.Today;
        _remitoRepository.GetByFiltersAsync(
            Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(),
            Arg.Is<DateTime?>(d => d == today), Arg.Is<DateTime?>(d => d == today),
            Arg.Any<CancellationToken>())
            .Returns(remitos);
    }

    private static Remito Remito(params decimal[] subtotales)
    {
        return new Remito
        {
            Id = Guid.NewGuid(),
            Lineas = subtotales
                .Select(s => new RemitoLinea { Id = Guid.NewGuid(), Subtotal = s })
                .ToList()
        };
    }

    [Fact]
    public async Task HandleAsync_ConDatos_CalculaLos10Kpis()
    {
        var recetaId = Guid.NewGuid();
        var produccionMes = new ProduccionEntity
        {
            Id = Guid.NewGuid(),
            RecetaId = recetaId,
            Receta = new Receta { Id = recetaId, Nombre = "Pan Rústico" },
            CantidadProducida = 10m,
            CostoTotal = 200m,
            Fecha = DateTime.Today
        };
        ConfigurarProduccionMes(new[] { produccionMes });
        ConfigurarProduccionDia(new[]
        {
            new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CantidadProducida = 5m, Fecha = DateTime.Today }
        });

        _insumoRepository.GetCriticosCountAsync(Arg.Any<CancellationToken>()).Returns(3);
        _productoTerminadoRepository.GetStockTotalAsync(Arg.Any<CancellationToken>()).Returns(7);
        _productoTerminadoRepository.GetProximosAVencerAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ProductoTerminado { Id = Guid.NewGuid() }, new ProductoTerminado { Id = Guid.NewGuid() } });
        ConfigurarRemitosMes(new[] { Remito(100m, 50m) });
        ConfigurarRemitosDia(new[] { Remito(30m) });
        _cuentaCorrienteProveedorRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(1000m);
        _cuentaCorrienteBarRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(500m);

        var result = await CreateHandler().HandleAsync(new GetDashboardQuery());

        result.IsSuccess.ShouldBeTrue();
        var kpis = result.Value;
        kpis.ProduccionDia.ShouldBe(5);
        kpis.ProduccionMes.ShouldBe(10m);
        kpis.StockInsumosCriticos.ShouldBe(3);
        kpis.StockProductosTerminados.ShouldBe(7);
        kpis.ProductosProximosAVencer.ShouldBe(2);
        kpis.VentasDia.ShouldBe(30m);
        kpis.VentasMes.ShouldBe(150m);
        kpis.DeudaProveedores.ShouldBe(1000m);
        kpis.DeudaBares.ShouldBe(500m);

        var costo = kpis.CostoPromedioPorProducto.ShouldHaveSingleItem();
        costo.ProductoId.ShouldBe(recetaId);
        costo.Nombre.ShouldBe("Pan Rústico");
        costo.CostoUnitario.ShouldBe(20m); // 200 / 10
    }

    [Fact]
    public async Task HandleAsync_SinDatos_DevuelveKpisEnCeroSinExcepcion()
    {
        ConfigurarProduccionMes(Array.Empty<ProduccionEntity>());
        ConfigurarProduccionDia(Array.Empty<ProduccionEntity>());
        _insumoRepository.GetCriticosCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetStockTotalAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetProximosAVencerAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductoTerminado>());
        ConfigurarRemitosMes(Array.Empty<Remito>());
        ConfigurarRemitosDia(Array.Empty<Remito>());
        _cuentaCorrienteProveedorRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _cuentaCorrienteBarRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);

        var result = await CreateHandler().HandleAsync(new GetDashboardQuery());

        result.IsSuccess.ShouldBeTrue();
        var kpis = result.Value;
        kpis.ProduccionDia.ShouldBe(0);
        kpis.ProduccionMes.ShouldBe(0m);
        kpis.StockInsumosCriticos.ShouldBe(0);
        kpis.StockProductosTerminados.ShouldBe(0);
        kpis.ProductosProximosAVencer.ShouldBe(0);
        kpis.VentasDia.ShouldBe(0m);
        kpis.VentasMes.ShouldBe(0m);
        kpis.DeudaProveedores.ShouldBe(0m);
        kpis.DeudaBares.ShouldBe(0m);
        kpis.CostoPromedioPorProducto.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ResuelveMetricasEnParalelo_UsaTaskWhenAll()
    {
        ConfigurarProduccionMes(Array.Empty<ProduccionEntity>());
        ConfigurarProduccionDia(Array.Empty<ProduccionEntity>());
        _insumoRepository.GetCriticosCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetStockTotalAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetProximosAVencerAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductoTerminado>());
        ConfigurarRemitosMes(Array.Empty<Remito>());
        ConfigurarRemitosDia(Array.Empty<Remito>());
        _cuentaCorrienteProveedorRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _cuentaCorrienteBarRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);

        var result = await CreateHandler().HandleAsync(new GetDashboardQuery());

        result.IsSuccess.ShouldBeTrue();
        await _produccionRepository.Received(2)
            .GetByFiltersAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>());
        await _insumoRepository.Received(1).GetCriticosCountAsync(Arg.Any<CancellationToken>());
        await _productoTerminadoRepository.Received(1).GetStockTotalAsync(Arg.Any<CancellationToken>());
        await _cuentaCorrienteProveedorRepository.Received(1).GetDeudaTotalAsync(Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.Received(1).GetDeudaTotalAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CostoPromedioSinProduccionPrevia_SoloDevuelveProducciones()
    {
        ConfigurarProduccionMes(Array.Empty<ProduccionEntity>());
        ConfigurarProduccionDia(Array.Empty<ProduccionEntity>());
        _insumoRepository.GetCriticosCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetStockTotalAsync(Arg.Any<CancellationToken>()).Returns(0);
        _productoTerminadoRepository.GetProximosAVencerAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductoTerminado>());
        ConfigurarRemitosMes(Array.Empty<Remito>());
        ConfigurarRemitosDia(Array.Empty<Remito>());
        _cuentaCorrienteProveedorRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);
        _cuentaCorrienteBarRepository.GetDeudaTotalAsync(Arg.Any<CancellationToken>()).Returns(0m);

        var result = await CreateHandler().HandleAsync(new GetDashboardQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.CostoPromedioPorProducto.ShouldBeEmpty();
    }
}
