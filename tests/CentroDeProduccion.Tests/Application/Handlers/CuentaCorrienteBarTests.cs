using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterCompensacion;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaCredito;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaDebito;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetEstadoCuenta;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetSaldo;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the bar's append-only current-account ledger: saldo is always the derived SUM of the
/// movements, GetEstadoCuenta recomputes a chronological running saldo, and the nota commands
/// enforce their sign conventions (NotaCredito negative, NotaDebito positive, Compensacion either).
/// </summary>
public class CuentaCorrienteBarTests
{
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteRepository = Substitute.For<ICuentaCorrienteBarRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<RegisterNotaDebitoCommand> _debitoValidator = new RegisterNotaDebitoCommandValidator();
    private readonly IValidator<RegisterNotaCreditoCommand> _creditoValidator = new RegisterNotaCreditoCommandValidator();
    private readonly IValidator<RegisterCompensacionCommand> _compensacionValidator = new RegisterCompensacionCommandValidator();

    private GetSaldoQueryHandler CreateSaldoHandler() => new(_cuentaCorrienteRepository, _barRepository);
    private GetEstadoCuentaQueryHandler CreateEstadoCuentaHandler() => new(_cuentaCorrienteRepository, _barRepository);
    private RegisterNotaDebitoCommandHandler CreateDebitoHandler() => new(
        _cuentaCorrienteRepository, _barRepository, _unitOfWork, _debitoValidator);
    private RegisterNotaCreditoCommandHandler CreateCreditoHandler() => new(
        _cuentaCorrienteRepository, _barRepository, _unitOfWork, _creditoValidator);
    private RegisterCompensacionCommandHandler CreateCompensacionHandler() => new(
        _cuentaCorrienteRepository, _barRepository, _unitOfWork, _compensacionValidator);

    private static Bar CrearBar(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        Estado = activo ? EstadoBar.Activo : EstadoBar.Inactivo
    };

    private static CuentaCorrienteBar Movimiento(Guid barId, TipoMovimientoCtaCteBar tipo, decimal monto, DateTime fecha) => new()
    {
        Id = Guid.NewGuid(),
        BarId = barId,
        TipoMovimiento = tipo,
        Monto = monto,
        Fecha = fecha
    };

    [Fact]
    public async Task GetSaldo_ConMovimientos_DevuelveLaSuma()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _cuentaCorrienteRepository.GetSaldoAsync(bar.Id, Arg.Any<CancellationToken>()).Returns(60000m);

        var result = await CreateSaldoHandler().HandleAsync(new GetSaldoQuery(bar.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(60000m);
    }

    [Fact]
    public async Task GetSaldo_SinMovimientos_DevuelveCero()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _cuentaCorrienteRepository.GetSaldoAsync(bar.Id, Arg.Any<CancellationToken>()).Returns(0m);

        var result = await CreateSaldoHandler().HandleAsync(new GetSaldoQuery(bar.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0m);
    }

    [Fact]
    public async Task GetEstadoCuenta_CalculaSaldoCorrelativoCronologico()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        // Deliberately out of chronological order — the handler must sort by Fecha first.
        var movimientos = new List<CuentaCorrienteBar>
        {
            Movimiento(bar.Id, TipoMovimientoCtaCteBar.Devolucion, -10000m, new DateTime(2025, 1, 3, 10, 0, 0)),
            Movimiento(bar.Id, TipoMovimientoCtaCteBar.Pago, -40000m, new DateTime(2025, 1, 2, 10, 0, 0)),
            Movimiento(bar.Id, TipoMovimientoCtaCteBar.Remito, 100000m, new DateTime(2025, 1, 1, 10, 0, 0))
        };
        _cuentaCorrienteRepository.GetByBarAsync(bar.Id, null, null, null, Arg.Any<CancellationToken>()).Returns(movimientos);

        var result = await CreateEstadoCuentaHandler().HandleAsync(new GetEstadoCuentaQuery(bar.Id, null, null, null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3);
        result.Value[0].TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Remito);
        result.Value[0].SaldoAcumulado.ShouldBe(100000m);
        result.Value[1].TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Pago);
        result.Value[1].SaldoAcumulado.ShouldBe(60000m);
        result.Value[2].TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Devolucion);
        result.Value[2].SaldoAcumulado.ShouldBe(50000m);
    }

    [Fact]
    public async Task GetEstadoCuenta_ReenviaFiltrosAlRepositorio()
    {
        var bar = CrearBar();
        var desde = new DateTime(2025, 1, 1);
        var hasta = new DateTime(2025, 1, 31);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        var filtrado = new List<CuentaCorrienteBar>
        {
            Movimiento(bar.Id, TipoMovimientoCtaCteBar.Pago, -40000m, new DateTime(2025, 1, 15, 10, 0, 0))
        };
        _cuentaCorrienteRepository.GetByBarAsync(bar.Id, TipoMovimientoCtaCteBar.Pago, desde, hasta, Arg.Any<CancellationToken>())
            .Returns(filtrado);

        var result = await CreateEstadoCuentaHandler().HandleAsync(
            new GetEstadoCuentaQuery(bar.Id, TipoMovimientoCtaCteBar.Pago, desde, hasta));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem();
        result.Value[0].SaldoAcumulado.ShouldBe(-40000m);
        await _cuentaCorrienteRepository.Received(1)
            .GetByBarAsync(bar.Id, TipoMovimientoCtaCteBar.Pago, desde, hasta, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotaCreditoNegativa_CreaMovimiento()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        CuentaCorrienteBar? movimiento = null;
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimiento = ci.Arg<CuentaCorrienteBar>());

        var result = await CreateCreditoHandler().HandleAsync(
            new RegisterNotaCreditoCommand(bar.Id, -500m, "Bonificación", null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Monto.ShouldBe(-500m);
        movimiento.ShouldNotBeNull();
        movimiento!.TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.NotaCredito);
        movimiento.Monto.ShouldBe(-500m);
        movimiento.BarId.ShouldBe(bar.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotaCreditoPositiva_ReturnsMontoInvalido()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateCreditoHandler().HandleAsync(
            new RegisterNotaCreditoCommand(bar.Id, 500m, "Bonificación", null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("MONTO_INVALIDO");
        result.Error.Message.ShouldBe("La nota de crédito debe tener monto negativo");
        await _cuentaCorrienteRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotaDebito_PositivoCreaMovimientoNegativoRechaza()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        CuentaCorrienteBar? movimiento = null;
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimiento = ci.Arg<CuentaCorrienteBar>());

        var positiva = await CreateDebitoHandler().HandleAsync(
            new RegisterNotaDebitoCommand(bar.Id, 500m, "Recargo", null));

        positiva.IsSuccess.ShouldBeTrue();
        positiva.Value.Monto.ShouldBe(500m);
        movimiento.ShouldNotBeNull();
        movimiento!.TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.NotaDebito);
        movimiento.Monto.ShouldBe(500m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        var negativa = await CreateDebitoHandler().HandleAsync(
            new RegisterNotaDebitoCommand(bar.Id, -500m, "Recargo", null));

        negativa.IsFailure.ShouldBeTrue();
        negativa.Error.Type.ShouldBe(ErrorType.Validation);
        negativa.Error.Code.ShouldBe("MONTO_INVALIDO");
        negativa.Error.Message.ShouldBe("La nota de débito debe tener monto positivo");
        await _cuentaCorrienteRepository.Received(1).AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Compensacion_AdmiteMontoPositivoYNegativo()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var movimientos = new List<CuentaCorrienteBar>();
        _cuentaCorrienteRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientos.Add(ci.Arg<CuentaCorrienteBar>()));

        var positiva = await CreateCompensacionHandler().HandleAsync(
            new RegisterCompensacionCommand(bar.Id, 800m, "Ajuste", null));
        var negativa = await CreateCompensacionHandler().HandleAsync(
            new RegisterCompensacionCommand(bar.Id, -800m, "Ajuste", null));

        positiva.IsSuccess.ShouldBeTrue();
        positiva.Value.Monto.ShouldBe(800m);
        negativa.IsSuccess.ShouldBeTrue();
        negativa.Value.Monto.ShouldBe(-800m);
        movimientos.Count.ShouldBe(2);
        movimientos.ShouldAllBe(m => m.TipoMovimiento == TipoMovimientoCtaCteBar.Compensacion);
        movimientos[0].Monto.ShouldBe(800m);
        movimientos[1].Monto.ShouldBe(-800m);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}