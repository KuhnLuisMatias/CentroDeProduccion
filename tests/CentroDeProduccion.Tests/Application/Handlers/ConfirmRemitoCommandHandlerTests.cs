using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the atomic confirm/send flow: every line is pre-checked for stock before any write,
/// then stock is decremented, one VentaBar/Reventa movement is registered per line, one
/// CuentaCorrienteBar Remito row is created and the remito transitions to Enviado in a single
/// SaveChanges. The first failing pre-check aborts with no partial writes.
/// </summary>
public class ConfirmRemitoCommandHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IMovimientoStockRepository _movimientoStockRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository = Substitute.For<ICuentaCorrienteBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<ConfirmRemitoCommand> _validator = new ConfirmRemitoCommandValidator();

    private ConfirmRemitoCommandHandler CreateHandler() => new(
        _remitoRepository, _barRepository, _productoTerminadoRepository, _insumoRepository,
        _movimientoStockRepository, _cuentaCorrienteBarRepository, _unitOfWork, _currentUser, _validator);

    private static Bar CrearBar(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        Estado = activo ? EstadoBar.Activo : EstadoBar.Inactivo
    };

    private static Remito CrearRemito(EstadoRemito estado, byte[] rowVersion, params RemitoLinea[] lineas) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 14,
        BarId = Guid.NewGuid(),
        Estado = estado,
        RowVersion = rowVersion,
        Lineas = lineas
    };

    private static RemitoLinea LineaPT(Guid productoId, decimal cantidad, decimal precioUnitario) => new()
    {
        Id = Guid.NewGuid(),
        TipoLinea = TipoLineaRemito.ProductoTerminado,
        ProductoTerminadoId = productoId,
        Cantidad = cantidad,
        PrecioUnitario = precioUnitario,
        Subtotal = cantidad * precioUnitario
    };

    private static RemitoLinea LineaInsumo(Guid insumoId, decimal cantidad, decimal precioUnitario) => new()
    {
        Id = Guid.NewGuid(),
        TipoLinea = TipoLineaRemito.Insumo,
        InsumoId = insumoId,
        Cantidad = cantidad,
        PrecioUnitario = precioUnitario,
        Subtotal = cantidad * precioUnitario
    };

    private static ProductoTerminado CrearProducto(decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Pan Rústico",
        CodigoSku = "PAN-001",
        StockActual = stock,
        FechaVencimiento = DateTime.UtcNow.AddDays(30)
    };

    private static Insumo CrearInsumo(decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina",
        CodigoSku = "HAR-001",
        StockActual = stock,
        PrecioUltimaCompra = 80m,
        UnidadConsumoId = Guid.NewGuid(),
        Activo = true
    };

    [Fact]
    public async Task HandleAsync_StockSuficiente_ConfirmaDescuentaStockYRegistraMovimientosYCtaCte()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var producto = CrearProducto(stock: 10m);
        var insumo = CrearInsumo(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion,
            LineaPT(producto.Id, 5m, 100m),   // 5 × 100 = 500
            LineaInsumo(insumo.Id, 3m, 80m)); // 3 × 80  = 240  → total 740
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var movimientos = new List<MovimientoStock>();
        _movimientoStockRepository.When(r => r.AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientos.Add(ci.Arg<MovimientoStock>()));
        var ctaCtes = new List<CuentaCorrienteBar>();
        _cuentaCorrienteBarRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => ctaCtes.Add(ci.Arg<CuentaCorrienteBar>()));

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoRemito.Enviado);
        result.Value.Total.ShouldBe(740m);
        remito.Estado.ShouldBe(EstadoRemito.Enviado);
        remito.FechaEnvio.ShouldNotBeNull();

        producto.StockActual.ShouldBe(5m); // 10 - 5
        insumo.StockActual.ShouldBe(7m);   // 10 - 3

        movimientos.Count.ShouldBe(2);
        var movPT = movimientos.Single(m => m.Tipo == TipoMovimientoStock.VentaBar);
        movPT.ProductoTerminadoId.ShouldBe(producto.Id);
        movPT.InsumoId.ShouldBeNull();
        movPT.Cantidad.ShouldBe(-5m);
        movPT.CantidadOriginal.ShouldBe(5m);
        movPT.FactorConversionAplicado.ShouldBe(1);
        movPT.DocumentoOrigen.ShouldBe(remito.Id.ToString());
        movPT.Motivo.ShouldContain(remito.NumeroRemito.ToString());
        var movInsumo = movimientos.Single(m => m.Tipo == TipoMovimientoStock.Reventa);
        movInsumo.InsumoId.ShouldBe(insumo.Id);
        movInsumo.ProductoTerminadoId.ShouldBeNull();
        movInsumo.Cantidad.ShouldBe(-3m);
        movInsumo.DocumentoOrigen.ShouldBe(remito.Id.ToString());

        var ctaCte = ctaCtes.ShouldHaveSingleItem();
        ctaCte.TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Remito);
        ctaCte.Monto.ShouldBe(740m);
        ctaCte.BarId.ShouldBe(remito.BarId);
        ctaCte.RemitoId.ShouldBe(remito.Id);
        ctaCte.Referencia.ShouldContain(remito.NumeroRemito.ToString());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StockPTInsuficiente_ReturnsStockInsuficienteSinEscrituras()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var producto = CrearProducto(stock: 2m);
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaPT(producto.Id, 10m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("STOCK_INSUFICIENTE");
        result.Error.Message.ShouldContain("requerido 10, disponible 2");
        remito.Estado.ShouldBe(EstadoRemito.Pendiente);
        producto.StockActual.ShouldBe(2m);
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProductoVencido_ReturnsProductoVencidoSinEscrituras()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var producto = CrearProducto(stock: 10m);
        producto.FechaVencimiento = DateTime.UtcNow.AddDays(-1);
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaPT(producto.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("PRODUCTO_VENCIDO");
        result.Error.Message.ShouldContain(producto.Nombre);
        remito.Estado.ShouldBe(EstadoRemito.Pendiente);
        producto.StockActual.ShouldBe(10m);
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StockInsumoInsuficiente_ReturnsStockInsuficienteInsumo()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var insumo = CrearInsumo(stock: 2m);
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaInsumo(insumo.Id, 10m, 80m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("STOCK_INSUFICIENTE_INSUMO");
        result.Error.Message.ShouldContain("requerido 10, disponible 2");
        result.Error.Message.ShouldContain("(en unidad de consumo)");
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PrimeraLineaFalla_NoEvaluaSegunda()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var productoFaltante = CrearProducto(stock: 1m);
        productoFaltante.Nombre = "Pan Integral";
        var productoSuficiente = CrearProducto(stock: 100m);
        productoSuficiente.Nombre = "Facturas";
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion,
            LineaPT(productoFaltante.Id, 10m, 100m),   // fails first
            LineaPT(productoSuficiente.Id, 10m, 100m)); // would pass, must never be evaluated
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(new[] { productoFaltante, productoSuficiente });

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("STOCK_INSUFICIENTE");
        result.Error.Message.ShouldContain(productoFaltante.Nombre);
        result.Error.Message.ShouldNotContain(productoSuficiente.Nombre);
        productoSuficiente.StockActual.ShouldBe(100m); // line 2 never touched
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemitoEnviado_ReturnsNoConfirmable()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Enviado, rowVersion, LineaPT(Guid.NewGuid(), 1m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_NO_CONFIRMABLE");
        await _barRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BarInactivo_ReturnsBarInactivo()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaPT(Guid.NewGuid(), 1m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar(activo: false));

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("BAR_INACTIVO");
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemitoSinLineas_ReturnsRemitoSinLineas()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion);
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_SIN_LINEAS");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RowVersionDistinta_ReturnsConcurrency()
    {
        var remito = CrearRemito(EstadoRemito.Pendiente, new byte[] { 1, 2, 3 }, LineaPT(Guid.NewGuid(), 1m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, new byte[] { 9, 9, 9 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
        await _barRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProductoTerminadoFaltante_ReturnsNotFound()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaPT(Guid.NewGuid(), 1m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(Array.Empty<ProductoTerminado>());

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("PRODUCTO_TERMINADO_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var producto = CrearProducto(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion, LineaPT(producto.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateHandler().HandleAsync(new ConfirmRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}