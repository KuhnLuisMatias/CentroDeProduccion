using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the return flow, the atomic counterpart of ConfirmRemito: the original remito must
/// be Enviado and its bar active, every line is pre-validated against the quantity originally
/// delivered minus everything already returned before any write, then stock is incremented, one
/// DevolucionBar movement is registered per line, one negative CuentaCorrienteBar Devolucion row
/// is created and the devolucion is added in a single SaveChanges.
/// </summary>
public class DevolucionCommandHandlerTests
{
    private readonly IDevolucionRepository _devolucionRepository = Substitute.For<IDevolucionRepository>();
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IMovimientoStockRepository _movimientoStockRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository = Substitute.For<ICuentaCorrienteBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<CreateDevolucionCommand> _validator = new CreateDevolucionCommandValidator();

    private CreateDevolucionCommandHandler CreateHandler() => new(
        _devolucionRepository, _remitoRepository, _barRepository, _productoTerminadoRepository,
        _movimientoStockRepository, _cuentaCorrienteBarRepository, _unitOfWork, _currentUser, _validator);

    private static Bar CrearBar(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        Estado = activo ? EstadoBar.Activo : EstadoBar.Inactivo
    };

    private static Remito CrearRemito(EstadoRemito estado, params RemitoLinea[] lineas) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 14,
        BarId = Guid.NewGuid(),
        Estado = estado,
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

    private static ProductoTerminado CrearProducto(decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Pan Rústico",
        CodigoSku = "PAN-001",
        StockActual = stock
    };

    private static CreateDevolucionCommand Command(Guid remitoId, params CreateDevolucionLineaCommand[] lineas) =>
        new(remitoId, null, null, lineas);

    private static CreateDevolucionLineaCommand Linea(Guid productoId, decimal cantidad) =>
        new(productoId, cantidad, null);

    [Fact]
    public async Task HandleAsync_DevolucionValida_IncrementaStockYRegistraMovimientoYCtaCteNegativa()
    {
        var usuarioId = Guid.NewGuid();
        var producto = CrearProducto(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(producto.Id, 5m, 100m)); // 5 × 100 = 500
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { producto });
        _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, decimal>());
        _devolucionRepository.GetNextNumeroAsync(Arg.Any<CancellationToken>()).Returns(1);
        _currentUser.UsuarioId.Returns(usuarioId);

        var movimientos = new List<MovimientoStock>();
        _movimientoStockRepository.When(r => r.AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientos.Add(ci.Arg<MovimientoStock>()));
        var ctaCtes = new List<CuentaCorrienteBar>();
        _cuentaCorrienteBarRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => ctaCtes.Add(ci.Arg<CuentaCorrienteBar>()));
        Devolucion? devolucion = null;
        _devolucionRepository.When(r => r.AddAsync(Arg.Any<Devolucion>(), Arg.Any<CancellationToken>()))
            .Do(ci => devolucion = ci.Arg<Devolucion>());

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(producto.Id, 3m)));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Total.ShouldBe(300m); // 3 × 100
        result.Value.RemitoId.ShouldBe(remito.Id);
        producto.StockActual.ShouldBe(13m); // 10 + 3

        devolucion.ShouldNotBeNull();
        devolucion!.Numero.ShouldBe(1);
        devolucion.RemitoId.ShouldBe(remito.Id);
        devolucion.CreadoPor.ShouldBe(usuarioId);
        devolucion.Lineas.Count.ShouldBe(1);
        devolucion.Lineas.Single().ProductoTerminadoId.ShouldBe(producto.Id);
        devolucion.Lineas.Single().Cantidad.ShouldBe(3m);

        var mov = movimientos.ShouldHaveSingleItem();
        mov.Tipo.ShouldBe(TipoMovimientoStock.DevolucionBar);
        mov.ProductoTerminadoId.ShouldBe(producto.Id);
        mov.InsumoId.ShouldBeNull();
        mov.Cantidad.ShouldBe(3m);
        mov.CantidadOriginal.ShouldBe(3m);
        mov.FactorConversionAplicado.ShouldBe(1);
        mov.Motivo.ShouldBe("Devolucion #1");
        mov.DocumentoOrigen.ShouldBe("Devolucion #1");
        mov.UsuarioId.ShouldBe(usuarioId);

        var ctaCte = ctaCtes.ShouldHaveSingleItem();
        ctaCte.TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Devolucion);
        ctaCte.Monto.ShouldBe(-300m);
        ctaCte.BarId.ShouldBe(remito.BarId);
        ctaCte.RemitoId.ShouldBe(remito.Id);
        ctaCte.DevolucionId.ShouldBe(devolucion.Id);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CantidadExcedeOriginal_ReturnsCantidadExcedeOriginal()
    {
        var producto = CrearProducto(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(producto.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { producto });
        _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>());

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(producto.Id, 6m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("CANTIDAD_EXCEDE_ORIGINAL");
        result.Error.Message.ShouldContain("requerido 6, disponible 5");
        producto.StockActual.ShouldBe(10m);
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _devolucionRepository.DidNotReceive().AddAsync(Arg.Any<Devolucion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DevueltoAcumuladoReduceDisponible_ReturnsRejectionConDisponible()
    {
        var producto = CrearProducto(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(producto.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { producto });
        _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, decimal> { [producto.Id] = 4m });

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(producto.Id, 2m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CANTIDAD_EXCEDE_ORIGINAL");
        result.Error.Message.ShouldContain("disponible 1"); // 5 − 4 ya devueltos
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemitoNoEnviado_ReturnsRemitoNoEnviado()
    {
        var remito = CrearRemito(EstadoRemito.Pendiente, LineaPT(Guid.NewGuid(), 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(Guid.NewGuid(), 1m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_NO_ENVIADO");
        await _barRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BarInactivo_ReturnsBarInactivo()
    {
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(Guid.NewGuid(), 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar(activo: false));

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(Guid.NewGuid(), 1m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("BAR_INACTIVO");
        await _productoTerminadoRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProductoNoEnRemito_ReturnsProductoNoEnRemito()
    {
        var productoEnRemito = CrearProducto(stock: 10m);
        productoEnRemito.Nombre = "Pan Rústico";
        var productoFuera = CrearProducto(stock: 10m);
        productoFuera.Nombre = "Facturas";
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(productoEnRemito.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { productoEnRemito, productoFuera });

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(productoFuera.Id, 1m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("PRODUCTO_NO_EN_REMITO");
        result.Error.Message.ShouldContain(productoFuera.Nombre);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PrimeraLineaFalla_NoEvaluaSegundaNiEscribe()
    {
        var productoFaltante = CrearProducto(stock: 10m);
        productoFaltante.Nombre = "Pan Integral";
        var productoSuficiente = CrearProducto(stock: 10m);
        productoSuficiente.Nombre = "Facturas";
        var remito = CrearRemito(EstadoRemito.Enviado,
            LineaPT(productoFaltante.Id, 5m, 100m),    // fails first: 6 > 5
            LineaPT(productoSuficiente.Id, 5m, 100m)); // would pass, must never be evaluated
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { productoFaltante, productoSuficiente });
        _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, decimal>());

        var result = await CreateHandler().HandleAsync(Command(remito.Id,
            Linea(productoFaltante.Id, 6m), Linea(productoSuficiente.Id, 2m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CANTIDAD_EXCEDE_ORIGINAL");
        result.Error.Message.ShouldContain(productoFaltante.Nombre);
        await _devolucionRepository.Received(1).GetTotalesDevueltosPorRemitoAsync(remito.Id, Arg.Any<CancellationToken>());
        
        productoSuficiente.StockActual.ShouldBe(10m); // line 2 never touched
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _devolucionRepository.DidNotReceive().AddAsync(Arg.Any<Devolucion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var producto = CrearProducto(stock: 10m);
        var remito = CrearRemito(EstadoRemito.Enviado, LineaPT(producto.Id, 5m, 100m));
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(remito.BarId).Returns(CrearBar());
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { producto });
        _devolucionRepository.GetTotalesDevueltosPorRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(new Dictionary<Guid, decimal>());
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateHandler().HandleAsync(Command(remito.Id, Linea(producto.Id, 1m)));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}
