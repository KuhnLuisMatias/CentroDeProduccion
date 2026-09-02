using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaDebito;
using CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the factura-de-compra invariants: MontoTotal is computed internally as the
/// sum of the insumo subtotals, each insumo line generates a Compra stock movement
/// (stock + weighted average update), and one CuentaCorrienteProveedor Compra movement
/// (+MontoTotal) is recorded atomically, creating the supplier debt.
/// </summary>
public class CreatePagoProveedorCommandHandlerTests
{
    private readonly IPagoProveedorRepository _pagoProveedorRepository = Substitute.For<IPagoProveedorRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IMovimientoStockRepository _movimientoStockRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository = Substitute.For<ICuentaCorrienteProveedorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<CreatePagoProveedorCommand> _validator = new CreatePagoProveedorCommandValidator();

    private CreatePagoProveedorCommandHandler CreateHandler() => new(
        _pagoProveedorRepository, _proveedorRepository, _insumoRepository,
        _movimientoStockRepository, _cuentaCorrienteRepository, _unitOfWork, _currentUser, _validator);

    private static Proveedor CrearProveedor(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        NombreRazonSocial = "Distribuidora Sur",
        Cuit = "20-12345678-9",
        Activo = activo
    };

    private static Insumo CrearInsumo(decimal stockActual = 0m, decimal factorConversion = 1m) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina 000",
        Activo = true,
        UnidadCompraId = Guid.NewGuid(),
        UnidadConsumoId = Guid.NewGuid(),
        FactorConversion = factorConversion,
        StockActual = stockActual,
        PrecioUltimaCompra = 0m
    };

    private static CreatePagoProveedorCommand Command(Guid proveedorId, Guid insumoId,
        decimal montoTotal, decimal cantidad, decimal precioUnitario) => new(
        proveedorId, DateTime.UtcNow, montoTotal, null,
        new[] { new PagoInsumoCommand(insumoId, cantidad, precioUnitario) });

    [Fact]
    public async Task HandleAsync_SumasCoincidentes_CreaFacturaStockYCtaCteCompra()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo(factorConversion: 10m); // 1 unidad de compra = 10 de consumo
        var usuarioId = Guid.NewGuid();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _pagoProveedorRepository.GetNextNumeroAsync().Returns(5);
        _currentUser.UsuarioId.Returns(usuarioId);

        PagoProveedor? pago = null;
        _pagoProveedorRepository.When(r => r.AddAsync(Arg.Any<PagoProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => pago = ci.Arg<PagoProveedor>());
        var movimientos = new List<MovimientoStock>();
        _movimientoStockRepository.When(r => r.AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientos.Add(ci.Arg<MovimientoStock>()));
        var movimientosCtaCte = new List<CuentaCorrienteProveedor>();
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientosCtaCte.Add(ci.Arg<CuentaCorrienteProveedor>()));

        // 2 unidades de compra × $50 = $100.
        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, insumo.Id, 100m, 2m, 50m));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Numero.ShouldBe(5);
        result.Value.MontoTotal.ShouldBe(100m);
        pago.ShouldNotBeNull();
        pago.Insumos.Sum(i => i.Cantidad * i.PrecioUnitario).ShouldBe(100m); // Σ subtotales == MontoTotal
        pago.CreadoPor.ShouldBe(usuarioId);

        var movimiento = movimientos.ShouldHaveSingleItem();
        movimiento.Tipo.ShouldBe(TipoMovimientoStock.Compra);
        movimiento.Cantidad.ShouldBe(20m); // 2 × factor 10, en unidad de consumo
        movimiento.CantidadOriginal.ShouldBe(2m);
        movimiento.PrecioUnitario.ShouldBe(50m);
        movimiento.Motivo.ShouldBe("Factura 5");
        movimiento.DocumentoOrigen.ShouldBe(pago.Id.ToString());
        movimiento.UsuarioId.ShouldBe(usuarioId);

        insumo.StockActual.ShouldBe(20m); // stock SUMADO
        insumo.PrecioUltimaCompra.ShouldBe(50m);

        var ctaCte = movimientosCtaCte.ShouldHaveSingleItem();
        ctaCte.TipoMovimiento.ShouldBe(TipoMovimientoCtaCte.Compra);
        ctaCte.Monto.ShouldBe(100m); // +MontoTotal: la factura CREA la deuda
        ctaCte.Referencia.ShouldBe("Factura 5");
        ctaCte.PagoProveedorId.ShouldBe(pago.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SumasDistintas_UsaSumaInsumosComoMontoTotal()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _pagoProveedorRepository.GetNextNumeroAsync().Returns(1);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        PagoProveedor? pago = null;
        _pagoProveedorRepository.When(r => r.AddAsync(Arg.Any<PagoProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => pago = ci.Arg<PagoProveedor>());
        var movimientosCtaCte = new List<CuentaCorrienteProveedor>();
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientosCtaCte.Add(ci.Arg<CuentaCorrienteProveedor>()));

        // Σ insumos = 2 × 40 = 80; command.MontoTotal = 100 (ignored).
        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, insumo.Id, 100m, 2m, 40m));

        result.IsSuccess.ShouldBeTrue();
        result.Value.MontoTotal.ShouldBe(80m);
        pago.ShouldNotBeNull();
        pago!.MontoTotal.ShouldBe(80m); // montoTotal computed from Σ subtotales

        var ctaCte = movimientosCtaCte.ShouldHaveSingleItem();
        ctaCte.Monto.ShouldBe(80m); // debt = Σ insumos
    }

    [Fact]
    public async Task HandleAsync_InsumoInexistente_ReturnsError()
    {
        var proveedor = CrearProveedor();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(Array.Empty<Insumo>());

        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, Guid.NewGuid(), 100m, 2m, 50m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUMO_NOT_FOUND");
        result.Error.Message.ShouldBe("Insumo no encontrado o inactivo");
        await _pagoProveedorRepository.DidNotReceive().AddAsync(Arg.Any<PagoProveedor>(), Arg.Any<CancellationToken>());
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProveedorInactivo_ReturnsError()
    {
        var proveedor = CrearProveedor(activo: false);
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { CrearInsumo() });

        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, Guid.NewGuid(), 100m, 2m, 50m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PROVEEDOR_NOT_FOUND");
        await _pagoProveedorRepository.DidNotReceive().AddAsync(Arg.Any<PagoProveedor>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies the append-only ledger nota entries: NotaDebito stores a positive Monto and
/// NotaCredito stores the negated Monto (command amount reduced from the supplier's debt).
/// </summary>
public class CuentaCorrienteNotaTests
{
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository = Substitute.For<ICuentaCorrienteProveedorRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<RegisterNotaDebitoCommand> _debitoValidator = new RegisterNotaDebitoCommandValidator();
    private readonly IValidator<RegisterNotaCreditoCommand> _creditoValidator = new RegisterNotaCreditoCommandValidator();

    private RegisterNotaDebitoCommandHandler CreateDebitoHandler() => new(
        _cuentaCorrienteRepository, _proveedorRepository, _unitOfWork, _debitoValidator);
    private RegisterNotaCreditoCommandHandler CreateCreditoHandler() => new(
        _cuentaCorrienteRepository, _proveedorRepository, _unitOfWork, _creditoValidator);

    private static Proveedor CrearProveedor() => new()
    {
        Id = Guid.NewGuid(),
        NombreRazonSocial = "Distribuidora Sur",
        Cuit = "20-12345678-9",
        Activo = true
    };

    [Fact]
    public async Task HandleAsync_NotaDebito_RegistraMovimientoPositivo()
    {
        var proveedor = CrearProveedor();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);

        CuentaCorrienteProveedor? movimiento = null;
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimiento = ci.Arg<CuentaCorrienteProveedor>());

        var result = await CreateDebitoHandler().HandleAsync(new RegisterNotaDebitoCommand(proveedor.Id, 500m, "Recargo"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Monto.ShouldBe(500m);
        movimiento.ShouldNotBeNull();
        movimiento!.TipoMovimiento.ShouldBe(TipoMovimientoCtaCte.NotaDebito);
        movimiento.Monto.ShouldBe(500m); // adds to the supplier's debt
        movimiento.ProveedorId.ShouldBe(proveedor.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotaCredito_RegistraMovimientoNegativo()
    {
        var proveedor = CrearProveedor();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);

        CuentaCorrienteProveedor? movimiento = null;
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteProveedor>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimiento = ci.Arg<CuentaCorrienteProveedor>());

        var result = await CreateCreditoHandler().HandleAsync(new RegisterNotaCreditoCommand(proveedor.Id, 500m, "Bonificación"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Monto.ShouldBe(-500m);
        movimiento.ShouldNotBeNull();
        movimiento!.TipoMovimiento.ShouldBe(TipoMovimientoCtaCte.NotaCredito);
        movimiento.Monto.ShouldBe(-500m); // reduces the supplier's debt
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
