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
/// Verifies the dashboard chart set: exactly seven charts are produced, each with non-null labels
/// and datasets, and empty data yields empty label/dataset arrays rather than throwing.
/// </summary>
public class GetDashboardChartsQueryHandlerTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteProveedorRepository = Substitute.For<ICuentaCorrienteProveedorRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();

    private GetDashboardChartsQueryHandler CreateHandler() => new(
        _produccionRepository, _insumoRepository, _remitoRepository, _ordenCompraRepository,
        _cuentaCorrienteProveedorRepository, _proveedorRepository);

    private void ConfigurarVacio()
    {
        _produccionRepository.GetByFiltersAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProduccionEntity>());
        _produccionRepository.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProduccionEntity>());
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Insumo>());
        _remitoRepository.GetByFiltersAsync(
            Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Remito>());
        _ordenCompraRepository.GetByFiltersAsync(
            Arg.Any<Guid?>(), Arg.Any<EstadoOrdenCompra?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrdenCompra>());
        _proveedorRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Proveedor>());
    }

    [Fact]
    public async Task HandleAsync_SinDatos_DevuelveSieteGraficosConLabelsYDatasetsNoNulos()
    {
        ConfigurarVacio();

        var result = await CreateHandler().HandleAsync(new GetDashboardChartsQuery());

        result.IsSuccess.ShouldBeTrue();
        var charts = result.Value.Charts;
        charts.Count.ShouldBe(7);
        foreach (var chart in charts)
        {
            chart.Labels.ShouldNotBeNull();
            chart.Datasets.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task HandleAsync_SinDatos_Graficos2A8VaciosYProduccionDiariaConLabelsDeDias()
    {
        ConfigurarVacio();

        var result = await CreateHandler().HandleAsync(new GetDashboardChartsQuery());

        result.IsSuccess.ShouldBeTrue();
        var charts = result.Value.Charts;
        // Chart 1 (Producción diaria) always emits one label per day of the month, with zeroed data.
        var diasDelMes = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
        charts[0].Labels.Count.ShouldBe(diasDelMes);
        charts[0].Datasets.ShouldAllBe(d => d.Data.All(v => v == 0m));
        // Chart 3 (Stock de insumos por nivel) always emits the three fixed level labels.
        charts[2].Labels.ShouldBe(new[] { "Crítico", "Bajo", "Normal" });
        // Chart 4 (Evolución de costos) always emits the twelve monthly labels, with zeroed data.
        charts[3].Labels.Count.ShouldBe(12);
        charts[3].Datasets.ShouldAllBe(d => d.Data.All(v => v == 0m));
        // The remaining charts derive labels from data, so empty data yields empty labels/datasets.
        foreach (var index in new[] { 1, 4, 5, 6 })
        {
            charts[index].Labels.ShouldBeEmpty();
            charts[index].Datasets.ShouldAllBe(d => d.Data.Count == 0);
        }
    }

    [Fact]
    public async Task HandleAsync_ProduccionDiaria_LabelsCoincidenConData()
    {
        ConfigurarVacio();
        var recetaId = Guid.NewGuid();
        _produccionRepository.GetByFiltersAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CantidadProducida = 4m, Fecha = DateTime.Today },
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CantidadProducida = 6m, Fecha = DateTime.Today }
            });

        var result = await CreateHandler().HandleAsync(new GetDashboardChartsQuery());

        result.IsSuccess.ShouldBeTrue();
        var produccionDiaria = result.Value.Charts[0];
        produccionDiaria.Type.ShouldBe("bar");
        produccionDiaria.Labels.ShouldNotBeEmpty();
        produccionDiaria.Labels.Count.ShouldBe(produccionDiaria.Datasets.Single().Data.Count);
        produccionDiaria.Datasets.Single().Data.ShouldContain(10m);
    }

    [Fact]
    public async Task HandleAsync_EvolucionCostos_SiempreTiene12EntradasMensuales()
    {
        ConfigurarVacio();
        var recetaId = Guid.NewGuid();
        _produccionRepository.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CostoTotal = 500m, Fecha = DateTime.Today }
            });

        var result = await CreateHandler().HandleAsync(new GetDashboardChartsQuery());

        result.IsSuccess.ShouldBeTrue();
        var evolucionCostos = result.Value.Charts[3];
        evolucionCostos.Type.ShouldBe("line");
        evolucionCostos.Labels.Count.ShouldBe(12);
        evolucionCostos.Datasets.Single().Data.Count.ShouldBe(12);
    }

    [Fact]
    public async Task HandleAsync_ConDatos_DevuelveSieteGraficosPoblados()
    {
        ConfigurarVacio();
        var recetaId = Guid.NewGuid();
        _produccionRepository.GetByFiltersAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CantidadProducida = 10m, Fecha = DateTime.Today }
            });
        _produccionRepository.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, CantidadProducida = 10m, CostoTotal = 100m, Fecha = DateTime.Today }
            });
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new Insumo { Id = Guid.NewGuid(), StockActual = 1m, StockMinimo = 10m }
            });

        var result = await CreateHandler().HandleAsync(new GetDashboardChartsQuery());

        result.IsSuccess.ShouldBeTrue();
        var charts = result.Value.Charts;
        charts.Count.ShouldBe(7);
        charts[0].Datasets.Single().Data.ShouldContain(10m);       // Producción diaria del mes
        charts[1].Labels.ShouldContain(string.Empty);              // Top 5 (nombre vacío sin Receta nav)
        charts[2].Labels.ShouldBe(new[] { "Crítico", "Bajo", "Normal" });
        charts[3].Labels.Count.ShouldBe(12);                       // Evolución de costos
    }
}
