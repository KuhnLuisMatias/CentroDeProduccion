using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Reports.Compras;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Reports.Produccion;
using CentroDeProduccion.Application.Features.Reports.Stock;
using CentroDeProduccion.Application.Features.Reports.Ventas;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;
using ProduccionEntity = CentroDeProduccion.Domain.Entities.Produccion;

namespace CentroDeProduccion.Tests.Application.Reports;

/// <summary>
/// Verifies the report query handlers of the "Reportes y dashboard" module (fase-6). Each
/// handler aggregates repository data into the DTO shape expected by the report table, applying
/// the documented defaults and edge-case behaviors.
/// </summary>
public class GetProduccionPeriodoReportQueryHandlerTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();

    private GetProduccionPeriodoReportQueryHandler CreateHandler() => new(_produccionRepository);

    [Fact]
    public async Task HandleAsync_SinRangoUsaUltimos30Dias()
    {
        var today = DateTime.Today;
        _produccionRepository.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProduccionEntity>());

        var result = await CreateHandler().HandleAsync(new GetProduccionPeriodoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Metadata.DateRangeFrom.ShouldBe(today.AddDays(-30));
        result.Value.Metadata.DateRangeTo.ShouldBe(today);
        await _produccionRepository.Received(1)
            .GetByDateRangeAsync(today.AddDays(-30), today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AgrupacionDia_UnaFilaPorDiaConTotales()
    {
        var d1 = DateTime.Today.AddDays(-1);
        var d2 = DateTime.Today;
        _produccionRepository.GetByDateRangeAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), Fecha = d1, CantidadProducida = 10m, CostoTotal = 100m },
                new ProduccionEntity { Id = Guid.NewGuid(), Fecha = d1, CantidadProducida = 5m, CostoTotal = 50m },
                new ProduccionEntity { Id = Guid.NewGuid(), Fecha = d2, CantidadProducida = 7m, CostoTotal = 70m }
            });

        var result = await CreateHandler().HandleAsync(new GetProduccionPeriodoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        var filaD1 = result.Value.Items.Single(i => i.PeriodoLabel == d1.ToString("dd/MM/yyyy"));
        filaD1.CantidadProducciones.ShouldBe(2);
        filaD1.CantidadProducida.ShouldBe(15m);
        filaD1.CostoTotal.ShouldBe(150m);
        var filaD2 = result.Value.Items.Single(i => i.PeriodoLabel == d2.ToString("dd/MM/yyyy"));
        filaD2.CantidadProducciones.ShouldBe(1);
        filaD2.CantidadProducida.ShouldBe(7m);
        filaD2.CostoTotal.ShouldBe(70m);
    }

    [Fact]
    public async Task HandleAsync_AgrupacionInvalida_DevuelveAgrupacionInvalida()
    {
        var result = await CreateHandler().HandleAsync(new GetProduccionPeriodoReportQuery(Agrupacion: "trimestre"));

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("AGRUPACION_INVALIDA");
    }
}

public class GetStockInsumosValoradoReportQueryHandlerTests
{
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();

    private GetStockInsumosValoradoReportQueryHandler CreateHandler() => new(_insumoRepository);

    private static Insumo Insumo(decimal stock, decimal pap, string unidad = "kg")
    {
        return new Insumo
        {
            Id = Guid.NewGuid(),
            Nombre = "Insumo",
            StockActual = stock,
            PrecioUltimaCompra = pap,
            UnidadConsumo = new UnidadMedida { Nombre = unidad },
            UnidadCompra = new UnidadMedida { Nombre = unidad }
        };
    }

    [Fact]
    public async Task HandleAsync_CalculaValorTotalYTotalValorizado()
    {
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Insumo(10m, 5m, "kg"),  // 50
                Insumo(3m, 20m, "un")   // 60
            });

        var result = await CreateHandler().HandleAsync(new GetStockInsumosValoradoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.TotalValorizado.ShouldBe(110m);
        dto.Items.Sum(i => i.ValorTotal).ShouldBe(110m);
        var primero = dto.Items.First();
        primero.ValorTotal.ShouldBe(60m); // ordenado desc
        primero.UnidadMedida.ShouldBe("un");
    }

    [Fact]
    public async Task HandleAsync_InsumoConStockCero_SeIncluyeConValorTotalCero()
    {
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Insumo(0m, 25m, "kg") });

        var result = await CreateHandler().HandleAsync(new GetStockInsumosValoradoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.ValorTotal.ShouldBe(0m);
        item.StockActual.ShouldBe(0m);
        result.Value.TotalValorizado.ShouldBe(0m);
    }

    [Fact]
    public async Task HandleAsync_SinInsumos_TotalesEnCero()
    {
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Insumo>());

        var result = await CreateHandler().HandleAsync(new GetStockInsumosValoradoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalValorizado.ShouldBe(0m);
    }
}

public class GetStockInsumosBajoMinimoReportQueryHandlerTests
{
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();

    private GetStockInsumosBajoMinimoReportQueryHandler CreateHandler() => new(_insumoRepository);

    private static Insumo Insumo(decimal stock, decimal minimo)
    {
        return new Insumo
        {
            Id = Guid.NewGuid(),
            Nombre = "Insumo",
            StockActual = stock,
            StockMinimo = minimo
        };
    }

    [Fact]
    public async Task HandleAsync_SoloIncluyeInsumosEnOBajoDelMinimo()
    {
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Insumo(5m, 10m),   // incluido
                Insumo(50m, 10m),  // excluido
                Insumo(10m, 10m)   // incluido (exactamente al mÃ­nimo)
            });

        var result = await CreateHandler().HandleAsync(new GetStockInsumosBajoMinimoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.ShouldNotContain(i => i.StockActual == 50m);
        var enMinimo = result.Value.Items.Single(i => i.StockActual == 10m);
        enMinimo.DiferenciaStock.ShouldBe(0m);
    }

    [Fact]
    public async Task HandleAsync_CalculaDiferenciaStockOrdenadaAscendente()
    {
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Insumo(7m, 10m),   // dif 3
                Insumo(1m, 10m)    // dif 9
            });

        var result = await CreateHandler().HandleAsync(new GetStockInsumosBajoMinimoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var items = result.Value.Items;
        items[0].DiferenciaStock.ShouldBe(3m);
        items[1].DiferenciaStock.ShouldBe(9m);
        items[0].DiferenciaStock.ShouldBeLessThan(items[1].DiferenciaStock);
    }
}

public class GetStockPTProximosAVencerReportQueryHandlerTests
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();

    private GetStockPTProximosAVencerReportQueryHandler CreateHandler() => new(_productoTerminadoRepository);

    private static ProductoTerminado Pt(DateTime vencimiento)
    {
        return new ProductoTerminado
        {
            Id = Guid.NewGuid(),
            Nombre = "Producto",
            StockActual = 4m,
            FechaVencimiento = vencimiento
        };
    }

    [Fact]
    public async Task HandleAsync_SoloIncluyeProductosQueVencenDesdeHoy()
    {
        var today = DateTime.Today;
        var horizon = today.AddDays(7);
        _productoTerminadoRepository.GetProximosAVencerAsync(horizon, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Pt(today.AddDays(-1)),  // excluido (ya vencido)
                Pt(today),              // incluido
                Pt(today.AddDays(7)),   // incluido (lÃ­mite)
                Pt(today.AddDays(10))   // dentro del rango devuelto por el repo; el lÃ­mite superior es responsabilidad del repo
            });

        var result = await CreateHandler().HandleAsync(new GetStockPTProximosAVencerReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(3);
        result.Value.Items.ShouldNotContain(i => i.FechaVencimiento == today.AddDays(-1));
    }

    [Fact]
    public async Task HandleAsync_OrdenaPorFechaVencimientoAscendente()
    {
        var today = DateTime.Today;
        _productoTerminadoRepository.GetProximosAVencerAsync(today.AddDays(7), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Pt(today.AddDays(6)),
                Pt(today.AddDays(2)),
                Pt(today.AddDays(4))
            });

        var result = await CreateHandler().HandleAsync(new GetStockPTProximosAVencerReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var items = result.Value.Items;
        items[0].FechaVencimiento.ShouldBe(today.AddDays(2));
        items[1].FechaVencimiento.ShouldBe(today.AddDays(4));
        items[2].FechaVencimiento.ShouldBe(today.AddDays(6));
        items[0].DiasParaVencer.ShouldBe(2);
    }
}

public class GetComprasPorProveedorReportQueryHandlerTests
{
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();

    private GetComprasPorProveedorReportQueryHandler CreateHandler() => new(_ordenCompraRepository);

    private static OrdenCompra Orden(Guid proveedorId, string nombre, EstadoOrdenCompra estado, params (decimal cant, decimal precio)[] items)
    {
        return new OrdenCompra
        {
            Id = Guid.NewGuid(),
            ProveedorId = proveedorId,
            Estado = estado,
            Proveedor = new Proveedor { Id = proveedorId, NombreRazonSocial = nombre },
            Items = items.Select(i => new OrdenCompraItem { CantidadPedida = i.cant, PrecioUnitario = i.precio }).ToList()
        };
    }

    [Fact]
    public async Task HandleAsync_AgrupaPorProveedorConTotalesCorrectos()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        _ordenCompraRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoOrdenCompra?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Orden(p1, "Proveedor A", EstadoOrdenCompra.Enviada, (2m, 10m), (1m, 5m)), // 25
                Orden(p1, "Proveedor A", EstadoOrdenCompra.Borrador, (3m, 10m)),          // 30
                Orden(p2, "Proveedor B", EstadoOrdenCompra.Cancelada, (1m, 100m))         // 100
            });

        var result = await CreateHandler().HandleAsync(new GetComprasPorProveedorReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        var a = result.Value.Items.Single(i => i.ProveedorId == p1);
        a.OrdenesCount.ShouldBe(2);
        a.TotalMonto.ShouldBe(55m);
        a.Pendientes.ShouldBe(1); // Enviada
        a.Canceladas.ShouldBe(0);
        var b = result.Value.Items.Single(i => i.ProveedorId == p2);
        b.TotalMonto.ShouldBe(100m);
        b.Canceladas.ShouldBe(1);
        result.Value.Items.First().ProveedorId.ShouldBe(p2); // ordenado por total desc
    }

    [Fact]
    public async Task HandleAsync_FiltroPorProveedorSePropagaAlRepositorio()
    {
        var proveedorId = Guid.NewGuid();
        _ordenCompraRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoOrdenCompra?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrdenCompra>());

        await CreateHandler().HandleAsync(new GetComprasPorProveedorReportQuery(ProveedorId: proveedorId));

        await _ordenCompraRepository.Received(1)
            .GetByFiltersAsync(proveedorId, null, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}

public class GetVentasPorBarReportQueryHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();

    private GetVentasPorBarReportQueryHandler CreateHandler() => new(_remitoRepository);

    private static Remito Remito(Guid barId, string barNombre, params decimal[] subtotales)
    {
        return new Remito
        {
            Id = Guid.NewGuid(),
            BarId = barId,
            Bar = new Bar { Id = barId, Nombre = barNombre },
            Estado = EstadoRemito.Enviado,
            Lineas = subtotales.Select(s => new RemitoLinea { Id = Guid.NewGuid(), Subtotal = s }).ToList()
        };
    }

    [Fact]
    public async Task HandleAsync_AgrupaRemitosEnviadosPorBarConSubtotales()
    {
        var bar1 = Guid.NewGuid();
        var bar2 = Guid.NewGuid();
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                Remito(bar1, "Bar Uno", 100m, 50m), // 150
                Remito(bar1, "Bar Uno", 30m),       // 30  -> total 180
                Remito(bar2, "Bar Dos", 70m)        // 70
            });

        var result = await CreateHandler().HandleAsync(new GetVentasPorBarReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var uno = result.Value.Items.Single(i => i.BarId == bar1);
        uno.RemitosCount.ShouldBe(2);
        uno.LineasCount.ShouldBe(3);
        uno.TotalSubtotal.ShouldBe(180m);
        var dos = result.Value.Items.Single(i => i.BarId == bar2);
        dos.TotalSubtotal.ShouldBe(70m);
        result.Value.Items.First().BarId.ShouldBe(bar1); // ordenado por total desc
    }

    [Fact]
    public async Task HandleAsync_SinRangoUsaMesCorrienteComoInicio()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Remito>());

        var result = await CreateHandler().HandleAsync(new GetVentasPorBarReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Metadata.DateRangeFrom.ShouldBe(monthStart);
        result.Value.Metadata.DateRangeTo.ShouldBe(today);
        await _remitoRepository.Received(1)
            .GetByFiltersAsync(null, EstadoRemito.Enviado, monthStart, today, Arg.Any<CancellationToken>());
    }
}

public class GetCostoProductoReportQueryHandlerTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly RecetaCostoResolver _recetaCostoResolver;

    public GetCostoProductoReportQueryHandlerTests()
    {
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Insumo>());
        _recetaCostoResolver = new RecetaCostoResolver(_recetaRepository, _insumoRepository);
    }

    private GetCostoProductoReportQueryHandler CreateHandler() => new(
        _produccionRepository, _recetaRepository, _recetaCostoResolver);

    private static Receta Receta(Guid id, string nombre) => new() { Id = id, Nombre = nombre };

    [Fact]
    public async Task HandleAsync_AgrupaCostosPorReceta()
    {
        var recetaId = Guid.NewGuid();
        _produccionRepository.GetByFiltersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, Receta = Receta(recetaId, "Pan"),
                    CostoTotalInsumos = 100m, CostoTotal = 100m },
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, Receta = Receta(recetaId, "Pan"),
                    CostoTotalInsumos = 100m, CostoTotal = 100m }
            });
        _recetaRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Receta(recetaId, "Pan") });

        var result = await CreateHandler().HandleAsync(new GetCostoProductoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.RecetaNombre.ShouldBe("Pan");
        item.CostoInsumos.ShouldBe(200m);
        item.CostoTotal.ShouldBe(200m);
        item.NumeroProducciones.ShouldBe(2);
        item.Observacion.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_RecetaSinProducciones_UsaCostoDeRecetaConObservacion()
    {
        var conProduccion = Guid.NewGuid();
        var sinProduccion = Guid.NewGuid();
        _produccionRepository.GetByFiltersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = conProduccion, Receta = Receta(conProduccion, "Con Prod"),
                    CostoTotal = 200m }
            });
        _recetaRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Receta(conProduccion, "Con Prod"), Receta(sinProduccion, "Sin Prod") });

        var result = await CreateHandler().HandleAsync(new GetCostoProductoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var fallback = result.Value.Items.Single(i => i.RecetaId == sinProduccion);
        fallback.CostoInsumos.ShouldBe(0m); // receta sin insumos -> costo estándar 0
        fallback.CostoTotal.ShouldBe(0m);
        fallback.NumeroProducciones.ShouldBe(0);
        fallback.Observacion.ShouldBe("sin costo de produccion registrado");
    }
}

public class GetRentabilidadProductoReportQueryHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly RecetaCostoResolver _recetaCostoResolver = new(
        Substitute.For<IRecetaRepository>(), Substitute.For<IInsumoRepository>());

    private GetRentabilidadProductoReportQueryHandler CreateHandler() => new(
        _remitoRepository, _productoTerminadoRepository, _produccionRepository, _recetaRepository, _recetaCostoResolver);

    private static RemitoLinea Linea(Guid? ptId, decimal precio, decimal cantidad)
    {
        return new RemitoLinea { Id = Guid.NewGuid(), ProductoTerminadoId = ptId, PrecioUnitario = precio, Cantidad = cantidad };
    }

    private static Remito Remito(params RemitoLinea[] lineas)
    {
        return new Remito
        {
            Id = Guid.NewGuid(),
            BarId = Guid.NewGuid(),
            Bar = new Bar { Id = Guid.NewGuid() },
            Estado = EstadoRemito.Enviado,
            Lineas = lineas.ToList()
        };
    }

    [Fact]
    public async Task HandleAsync_IngresosDesdeRemitosEnviadosYRentabilidadCalculada()
    {
        var ptId = Guid.NewGuid();
        var recetaId = Guid.NewGuid();
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Remito(Linea(ptId, 10m, 5m), Linea(ptId, 10m, 5m)) }); // ingresos 100
        _productoTerminadoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ProductoTerminado { Id = ptId, Nombre = "Cerveza" } });
        _produccionRepository.GetByFiltersWithSalidasAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ProduccionEntity { Id = Guid.NewGuid(), RecetaId = recetaId, Receta = Receta(recetaId, "Receta Cerveza"),
                    CostoTotal = 60m,
                    Salidas = new List<ProduccionSalida> { new() { ProductoTerminadoId = ptId } } }
            });
        _recetaRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Receta(recetaId, "Receta Cerveza") });

        var result = await CreateHandler().HandleAsync(new GetRentabilidadProductoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.ProductoTerminadoNombre.ShouldBe("Cerveza");
        item.Ingresos.ShouldBe(100m);
        item.Costos.ShouldBe(60m);
        item.Rentabilidad.ShouldBe(40m);
        item.MargenPorcentaje.ShouldBe(40m);
        item.Observacion.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ProductoSinReceta_CostoCeroConObservacion()
    {
        var ptId = Guid.NewGuid();
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Remito(Linea(ptId, 20m, 3m)) }); // ingresos 60
        _productoTerminadoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ProductoTerminado { Id = ptId, Nombre = "Sin Receta" } });
        _produccionRepository.GetByFiltersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProduccionEntity>());
        _recetaRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Receta>());

        var result = await CreateHandler().HandleAsync(new GetRentabilidadProductoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.Ingresos.ShouldBe(60m);
        item.Costos.ShouldBe(0m);
        item.Rentabilidad.ShouldBe(60m);
        item.Observacion.ShouldBe("sin costo registrado");
    }

    [Fact]
    public async Task HandleAsync_SinIngresos_DevuelveListaVacia()
    {
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Remito>());

        var result = await CreateHandler().HandleAsync(new GetRentabilidadProductoReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    private static Receta Receta(Guid id, string nombre) => new() { Id = id, Nombre = nombre };
}

public class GetRentabilidadBarReportQueryHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly RecetaCostoResolver _recetaCostoResolver = new(
        Substitute.For<IRecetaRepository>(), Substitute.For<IInsumoRepository>());

    private GetRentabilidadBarReportQueryHandler CreateHandler() => new(
        _remitoRepository, _productoTerminadoRepository, _produccionRepository, _recetaRepository, _recetaCostoResolver);

    [Fact]
    public async Task HandleAsync_AgrupaIngresosPorBar()
    {
        var bar1 = Guid.NewGuid();
        var bar2 = Guid.NewGuid();
        var ptId = Guid.NewGuid();
        _remitoRepository.GetByFiltersAsync(
                Arg.Any<Guid?>(), Arg.Any<EstadoRemito?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new Remito
                {
                    Id = Guid.NewGuid(), BarId = bar1, Bar = new Bar { Id = bar1, Nombre = "Bar Uno" }, Estado = EstadoRemito.Enviado,
                    Lineas = new List<RemitoLinea>
                    {
                        new() { Id = Guid.NewGuid(), ProductoTerminadoId = ptId, PrecioUnitario = 10m, Cantidad = 5m }, // 50
                        new() { Id = Guid.NewGuid(), ProductoTerminadoId = ptId, PrecioUnitario = 10m, Cantidad = 3m }  // 30 -> 80
                    }
                },
                new Remito
                {
                    Id = Guid.NewGuid(), BarId = bar2, Bar = new Bar { Id = bar2, Nombre = "Bar Dos" }, Estado = EstadoRemito.Enviado,
                    Lineas = new List<RemitoLinea>
                    {
                        new() { Id = Guid.NewGuid(), ProductoTerminadoId = ptId, PrecioUnitario = 10m, Cantidad = 4m } // 40
                    }
                }
            });
        _produccionRepository.GetByFiltersAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<Guid?>(), Arg.Any<EstadoProduccion?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProduccionEntity>());
        _recetaRepository.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Receta>());

        var result = await CreateHandler().HandleAsync(new GetRentabilidadBarReportQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        var uno = result.Value.Items.Single(i => i.BarId == bar1);
        uno.Ingresos.ShouldBe(80m);
        uno.Costos.ShouldBe(0m); // sin producciones -> sin costo
        uno.Rentabilidad.ShouldBe(80m);
        uno.MargenPorcentaje.ShouldBe(100m);
        var dos = result.Value.Items.Single(i => i.BarId == bar2);
        dos.Ingresos.ShouldBe(40m);
        result.Value.Items.First().BarId.ShouldBe(bar1); // ordenado por ingresos desc
    }
}
