using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegistrarPagoProveedor;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the supplier payment flow: the server computes MontoTotal from the payment method
/// lines, a payment may partially settle a factura but never exceed its outstanding balance
/// (PAGO_EXCEDE_PENDIENTE), exactly ONE negative CuentaCorrienteProveedor Pago row is written
/// (generic when no factura), inactive suppliers are rejected, and EstadoPago is derived from
/// the ledger (Pendiente/Parcial/Pagada), never stored.
/// </summary>
public class RegistrarPagoProveedorCommandHandlerTests
{
    private readonly IPagoAProveedorRepository _pagoAProveedorRepository = Substitute.For<IPagoAProveedorRepository>();
    private readonly IPagoProveedorRepository _pagoProveedorRepository = Substitute.For<IPagoProveedorRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository = Substitute.For<ICuentaCorrienteProveedorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private RegistrarPagoProveedorCommandHandler CreateHandler() => new(
        _pagoAProveedorRepository, _pagoProveedorRepository, _proveedorRepository,
        _cuentaCorrienteRepository, _unitOfWork, _currentUser,
        new RegistrarPagoProveedorCommandValidator());

    public RegistrarPagoProveedorCommandHandlerTests()
    {
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
    }

    private static PagoProveedor CreateFactura(decimal montoTotal)
        => new()
        {
            Id = Guid.NewGuid(),
            Numero = 7,
            ProveedorId = Guid.NewGuid(),
            FechaPago = RelojDeNegocio.Ahora,
            MontoTotal = montoTotal
        };

    // ── Pago sin factura: one generic negative Pago row ─────────────────────────────────────

    [Fact]
    public async Task Handle_WithoutFactura_WritesGenericPagoLedgerRow()
    {
        var proveedor = new Proveedor { Id = Guid.NewGuid(), NombreRazonSocial = "Distribuidora SA", Activo = true };
        _proveedorRepository.GetByIdAsync(proveedor.Id, Arg.Any<CancellationToken>()).Returns(proveedor);

        var command = new RegistrarPagoProveedorCommand(
            proveedor.Id, RelojDeNegocio.Ahora, null, "Adelanto",
            [new RegistrarPagoMetodoDto((int)MetodoPago.Efectivo, 100m, null),
             new RegistrarPagoMetodoDto((int)MetodoPago.Transferencia, 50m, "TB-123")]);

        var result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MontoTotal.ShouldBe(150m); // server-computed

        await _cuentaCorrienteRepository.Received(1).AddAsync(
            Arg.Do<CuentaCorrienteProveedor>(cc =>
            {
                cc.TipoMovimiento.ShouldBe(TipoMovimientoCtaCte.Pago);
                cc.Monto.ShouldBe(-150m);
                cc.Referencia.ShouldBe("Adelanto"); // observaciones become the reference
                cc.PagoProveedorId.ShouldBeNull();
                cc.Fecha.ShouldBe(command.Fecha);
            }), Arg.Any<CancellationToken>());

        await _pagoAProveedorRepository.Received(1).AddAsync(
            Arg.Do<PagoAProveedor>(p =>
            {
                p.MontoTotal.ShouldBe(150m);
                p.FacturaId.ShouldBeNull();
                p.Medios.Count.ShouldBe(2);
                p.Medios.Single(m => m.Tipo == MetodoPago.Transferencia).Referencia.ShouldBe("TB-123");
            }), Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Pago parcial de una factura: allowed, reports the remaining balance ──────────────────

    [Fact]
    public async Task Handle_PagoParcialDeFactura_SucceedsAndReportsPendiente()
    {
        var proveedor = new Proveedor { Id = Guid.NewGuid(), NombreRazonSocial = "Distribuidora SA", Activo = true };
        var factura = CreateFactura(100m);
        factura.ProveedorId = proveedor.Id;
        _proveedorRepository.GetByIdAsync(proveedor.Id, Arg.Any<CancellationToken>()).Returns(proveedor);
        _pagoProveedorRepository.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);
        _cuentaCorrienteRepository.GetPagosAplicadosAFacturaAsync(factura.Id, Arg.Any<CancellationToken>())
            .Returns(30m); // already settled 30

        var command = new RegistrarPagoProveedorCommand(
            proveedor.Id, RelojDeNegocio.Ahora, factura.Id, null,
            [new RegistrarPagoMetodoDto((int)MetodoPago.Efectivo, 50m, null)]);

        var result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FacturaPagado.ShouldBe(80m);
        result.Value.FacturaPendiente.ShouldBe(20m);
        result.Value.FacturaMontoTotal.ShouldBe(100m);

        await _cuentaCorrienteRepository.Received(1).AddAsync(
            Arg.Do<CuentaCorrienteProveedor>(cc =>
            {
                cc.TipoMovimiento.ShouldBe(TipoMovimientoCtaCte.Pago);
                cc.Monto.ShouldBe(-50m);
                cc.Referencia.ShouldBe("Pago factura N° 7");
                cc.PagoProveedorId.ShouldBe(factura.Id);
            }), Arg.Any<CancellationToken>());
    }

    // ── Pago que excede el pendiente: rejected, nothing saved ────────────────────────────────

    [Fact]
    public async Task Handle_MontoExcedePendiente_FailsWithPAGO_EXCEDE_PENDIENTE()
    {
        var proveedor = new Proveedor { Id = Guid.NewGuid(), NombreRazonSocial = "Distribuidora SA", Activo = true };
        var factura = CreateFactura(100m);
        factura.ProveedorId = proveedor.Id;
        _proveedorRepository.GetByIdAsync(proveedor.Id, Arg.Any<CancellationToken>()).Returns(proveedor);
        _pagoProveedorRepository.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);
        _cuentaCorrienteRepository.GetPagosAplicadosAFacturaAsync(factura.Id, Arg.Any<CancellationToken>())
            .Returns(30m); // pendiente 70

        var command = new RegistrarPagoProveedorCommand(
            proveedor.Id, RelojDeNegocio.Ahora, factura.Id, null,
            [new RegistrarPagoMetodoDto((int)MetodoPago.Efectivo, 80m, null)]);

        var result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("PAGO_EXCEDE_PENDIENTE");
        result.Error.Message.ShouldContain("70");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Proveedor inactivo: rejected ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProveedorInactivo_Fails()
    {
        var proveedor = new Proveedor { Id = Guid.NewGuid(), NombreRazonSocial = "Inactivo SA", Activo = false };
        _proveedorRepository.GetByIdAsync(proveedor.Id, Arg.Any<CancellationToken>()).Returns(proveedor);

        var command = new RegistrarPagoProveedorCommand(
            proveedor.Id, RelojDeNegocio.Ahora, null, null,
            [new RegistrarPagoMetodoDto((int)MetodoPago.Efectivo, 10m, null)]);

        var result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("PROVEEDOR_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Factura de otro proveedor: rejected ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_FacturaDeOtroProveedor_Fails()
    {
        var proveedor = new Proveedor { Id = Guid.NewGuid(), NombreRazonSocial = "Distribuidora SA", Activo = true };
        var factura = CreateFactura(100m); // belongs to another proveedor
        _proveedorRepository.GetByIdAsync(proveedor.Id, Arg.Any<CancellationToken>()).Returns(proveedor);
        _pagoProveedorRepository.GetByIdAsync(factura.Id, Arg.Any<CancellationToken>()).Returns(factura);

        var command = new RegistrarPagoProveedorCommand(
            proveedor.Id, RelojDeNegocio.Ahora, factura.Id, null,
            [new RegistrarPagoMetodoDto((int)MetodoPago.Efectivo, 10m, null)]);

        var result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("FACTURA_OTRO_PROVEEDOR");
    }

    // ── EstadoPago derivation (computed from the ledger, never stored) ───────────────────────

    [Fact]
    public void Map_SinPagos_EsPendiente()
    {
        var response = GetPagoByIdQueryHandler.Map(CreateFactura(100m), 0m);

        response.EstadoPago.ShouldBe("Pendiente");
        response.MontoPagado.ShouldBe(0m);
        response.MontoPendiente.ShouldBe(100m);
    }

    [Fact]
    public void Map_PagosAcumuladosMenoresAlTotal_EsParcial()
    {
        var response = GetPagoByIdQueryHandler.Map(CreateFactura(100m), 30m);

        response.EstadoPago.ShouldBe("Parcial");
        response.MontoPendiente.ShouldBe(70m);
    }

    [Fact]
    public void Map_PagosAcumuladosAlTotal_EsPagada()
    {
        var response = GetPagoByIdQueryHandler.Map(CreateFactura(100m), 100m);

        response.EstadoPago.ShouldBe("Pagada");
        response.MontoPendiente.ShouldBe(0m);
    }

    [Fact]
    public void Map_PagosAcumuladosMayoresAlTotal_EsPagadaSinPendienteNegativo()
    {
        var response = GetPagoByIdQueryHandler.Map(CreateFactura(100m), 120m);

        response.EstadoPago.ShouldBe("Pagada");
        response.MontoPendiente.ShouldBe(0m);
        response.MontoPagado.ShouldBe(120m);
    }
}
