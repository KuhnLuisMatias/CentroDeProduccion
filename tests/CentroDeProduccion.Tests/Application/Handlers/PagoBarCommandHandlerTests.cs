using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the bar-payment invariants: the sums of the payment methods and of the allocations
/// must both equal MontoTotal, no allocation may exceed a remito's outstanding debt, the remito
/// must be Enviado, and one CuentaCorrienteBar Pago movement (-MontoTotal) is created atomically
/// with the payment.
/// </summary>
public class PagoBarCommandHandlerTests
{
    private readonly IPagoBarRepository _pagoBarRepository = Substitute.For<IPagoBarRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteBarRepository = Substitute.For<ICuentaCorrienteBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<CreatePagoBarCommand> _validator = new CreatePagoBarCommandValidator();

    private CreatePagoBarCommandHandler CreateHandler() => new(
        _pagoBarRepository, _barRepository, _remitoRepository, _cuentaCorrienteBarRepository,
        _unitOfWork, _currentUser, _validator);

    private static Bar CrearBar(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        Estado = activo ? EstadoBar.Activo : EstadoBar.Inactivo
    };

    private static Remito CrearRemito(EstadoRemito estado, decimal total) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 9,
        BarId = Guid.NewGuid(),
        Estado = estado,
        Lineas = new List<RemitoLinea>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TipoLinea = TipoLineaRemito.ProductoTerminado,
                ProductoTerminadoId = Guid.NewGuid(),
                Cantidad = 10m,
                PrecioUnitario = total / 10m,
                Subtotal = total
            }
        }
    };

    private static CreatePagoBarCommand Command(Guid barId, Guid remitoId, decimal montoTotal,
        decimal metodoMonto, decimal itemMonto) => new(
        barId, DateTime.UtcNow, montoTotal, null,
        new[] { new PagoBarMetodoCommand(MetodoPago.Efectivo, metodoMonto, null) },
        new[] { new PagoBarItemCommand(remitoId, itemMonto) });

    [Fact]
    public async Task HandleAsync_SumasCoincidentes_CreaPagoBarYCtaCteNegativa()
    {
        var usuarioId = Guid.NewGuid();
        var bar = CrearBar();
        var remito = CrearRemito(EstadoRemito.Enviado, 1000m);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _pagoBarRepository.GetTotalPaidForRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(0m);
        _cuentaCorrienteBarRepository.GetDevolucionTotalByRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(0m);
        _pagoBarRepository.GetNextNumeroAsync(Arg.Any<CancellationToken>()).Returns(5);
        _currentUser.UsuarioId.Returns(usuarioId);

        PagoBar? pagoBar = null;
        _pagoBarRepository.When(r => r.AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => pagoBar = ci.Arg<PagoBar>());
        var movimientosCtaCte = new List<CuentaCorrienteBar>();
        _cuentaCorrienteBarRepository.When(r => r.AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientosCtaCte.Add(ci.Arg<CuentaCorrienteBar>()));

        var result = await CreateHandler().HandleAsync(Command(bar.Id, remito.Id, 1000m, 1000m, 1000m));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Numero.ShouldBe(5);
        result.Value.BarId.ShouldBe(bar.Id);
        result.Value.MontoTotal.ShouldBe(1000m);
        pagoBar.ShouldNotBeNull();
        pagoBar!.BarId.ShouldBe(bar.Id);
        pagoBar.CreadoPor.ShouldBe(usuarioId);
        pagoBar.Metodos.Sum(m => m.Monto).ShouldBe(1000m); // Σ métodos == MontoTotal
        pagoBar.Items.Sum(i => i.MontoAplicado).ShouldBe(1000m); // Σ asignaciones == MontoTotal
        pagoBar.Items.Single().RemitoId.ShouldBe(remito.Id);
        var ctaCte = movimientosCtaCte.ShouldHaveSingleItem();
        ctaCte.TipoMovimiento.ShouldBe(TipoMovimientoCtaCteBar.Pago);
        ctaCte.Monto.ShouldBe(-1000m);
        ctaCte.Referencia.ShouldBe("PagoBar #5");
        ctaCte.PagoBarId.ShouldBe(pagoBar.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MetodosNoCoinciden_ReturnsAsignacionInvalida()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, Guid.NewGuid(), 1000m, 900m, 1000m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("ASIGNACION_INVALIDA");
        result.Error.Message.ShouldContain("métodos de pago");
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AsignacionesNoCoinciden_ReturnsAsignacionInvalida()
    {
        var bar = CrearBar();
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, Guid.NewGuid(), 1000m, 1000m, 900m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("ASIGNACION_INVALIDA");
        result.Error.Message.ShouldContain("asignaciones");
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AsignacionExcedeDeuda_ReturnsAsignacionExcedeDeuda()
    {
        var bar = CrearBar();
        var remito = CrearRemito(EstadoRemito.Enviado, 1000m); // deuda total $1000
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _pagoBarRepository.GetTotalPaidForRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(800m);
        _cuentaCorrienteBarRepository.GetDevolucionTotalByRemitoAsync(remito.Id, Arg.Any<CancellationToken>()).Returns(0m);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, remito.Id, 1000m, 1000m, 1000m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("ASIGNACION_EXCEDE_DEUDA");
        result.Error.Message.ShouldContain("excede la deuda pendiente");
        result.Error.Message.ShouldContain("200"); // $1000 − $800 ya pagados
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemitoNoEnviado_ReturnsRemitoEstadoInvalido()
    {
        var bar = CrearBar();
        var remito = CrearRemito(EstadoRemito.Pendiente, 1000m);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, remito.Id, 1000m, 1000m, 1000m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_ESTADO_INVALIDO");
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _cuentaCorrienteBarRepository.DidNotReceive().AddAsync(Arg.Any<CuentaCorrienteBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BarInactivo_ReturnsBarNotFound()
    {
        var bar = CrearBar(activo: false);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, Guid.NewGuid(), 1000m, 1000m, 1000m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("BAR_NOT_FOUND");
        result.Error.Message.ShouldBe("Bar no encontrado o inactivo");
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MontoTotalNoPositivo_ReturnsValidationError()
    {
        var result = await CreateHandler().HandleAsync(Command(Guid.NewGuid(), Guid.NewGuid(), 0m, 1000m, 1000m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldBe("El monto total debe ser mayor a cero");
        await _pagoBarRepository.DidNotReceive().AddAsync(Arg.Any<PagoBar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}