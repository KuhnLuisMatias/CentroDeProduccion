using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the guided inventory ("toma de inventario") flow: opening a session pre-fills one
/// conteo per active item with system stock (ConteoOk), recording a count recomputes
/// Diferencia/ConteoOk, and confirming reconciles stock to the counted quantity, creating one
/// AjustePositivo/AjusteNegativo movement per difference and closing the session atomically.
/// </summary>
public class InventarioCommandHandlerTests
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository = Substitute.For<IInventarioSesionRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IMovimientoStockRepository _movimientoStockRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private CreateInventarioSesionCommandHandler CreateHandler() => new(
        _inventarioSesionRepository, _insumoRepository, _productoTerminadoRepository,
        _unitOfWork, _currentUser, new CreateInventarioSesionCommandValidator());

    private RegistrarConteoCommandHandler CreateRegistrarHandler() => new(
        _inventarioSesionRepository, _unitOfWork, new RegistrarConteoCommandValidator());

    private ConfirmInventarioSesionCommandHandler CreateConfirmHandler() => new(
        _inventarioSesionRepository, _insumoRepository, _productoTerminadoRepository,
        _movimientoStockRepository, CrearCostoResolver(), _unitOfWork, _currentUser,
        new ConfirmInventarioSesionCommandValidator());

    /// <summary>The resolver is harmless here: PT fixtures have no RecetaId → cost 0.</summary>
    private static ProductoTerminadoCostoResolver CrearCostoResolver()
    {
        var recetaRepo = Substitute.For<IRecetaRepository>();
        var insumoRepo = Substitute.For<IInsumoRepository>();
        return new ProductoTerminadoCostoResolver(recetaRepo, new RecetaCostoResolver(recetaRepo, insumoRepo));
    }

    private static Insumo CrearInsumo(decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina",
        CodigoSku = "HAR-001",
        UnidadConsumoId = Guid.NewGuid(),
        StockActual = stock,
        PrecioUltimaCompra = 80m,
        Activo = true
    };

    private static ProductoTerminado CrearProducto(decimal stock) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Pan Rústico",
        CodigoSku = "PAN-001",
        UnidadMedidaId = Guid.NewGuid(),
        StockActual = stock,
        Activo = true
    };

    private static InventarioSesion CrearSesion(byte[] rowVersion, params InventarioConteo[] conteos) => new()
    {
        Id = Guid.NewGuid(),
        TipoInventario = TipoInventario.Insumo,
        Estado = EstadoInventario.Abierta,
        ResponsableId = Guid.NewGuid(),
        RowVersion = rowVersion,
        Conteos = conteos
    };

    private static InventarioConteo Conteo(Guid? insumoId, Guid? ptId, decimal sistema, decimal contada) => new()
    {
        Id = Guid.NewGuid(),
        InsumoId = insumoId,
        ProductoTerminadoId = ptId,
        CantidadSistema = sistema,
        CantidadContada = contada
    };

    [Fact]
    public async Task CreateInsumos_CreaSesionYUnConteoPorInsumoConStockSistema()
    {
        var responsableId = Guid.NewGuid();
        var insumos = new[] { CrearInsumo(10m), CrearInsumo(25m) };
        _insumoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(insumos);
        InventarioSesion? session = null;
        _inventarioSesionRepository.When(r => r.AddAsync(Arg.Any<InventarioSesion>(), Arg.Any<CancellationToken>()))
            .Do(ci => session = ci.Arg<InventarioSesion>());

        var result = await CreateHandler().HandleAsync(
            new CreateInventarioSesionCommand(TipoInventario.Insumo, responsableId, "notas"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Tipo.ShouldBe(TipoInventario.Insumo);
        result.Value.Estado.ShouldBe(EstadoInventario.Abierta);
        result.Value.TotalItems.ShouldBe(2);
        session.ShouldNotBeNull();
        session!.ResponsableId.ShouldBe(responsableId);
        session.Notas.ShouldBe("notas");
        session.Conteos.Count.ShouldBe(2);
        foreach (var insumo in insumos)
        {
            var conteo = session.Conteos.Single(c => c.InsumoId == insumo.Id);
            conteo.CantidadSistema.ShouldBe(insumo.StockActual);
            conteo.CantidadContada.ShouldBe(insumo.StockActual);
            conteo.ConteoOk.ShouldBeTrue();
        }
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProductosTerminados_CreaSesionYUnConteoPorProductoConStockSistema()
    {
        var responsableId = Guid.NewGuid();
        var productos = new[] { CrearProducto(5m), CrearProducto(7m) };
        _productoTerminadoRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(productos);
        InventarioSesion? session = null;
        _inventarioSesionRepository.When(r => r.AddAsync(Arg.Any<InventarioSesion>(), Arg.Any<CancellationToken>()))
            .Do(ci => session = ci.Arg<InventarioSesion>());

        var result = await CreateHandler().HandleAsync(
            new CreateInventarioSesionCommand(TipoInventario.ProductoTerminado, responsableId, null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Tipo.ShouldBe(TipoInventario.ProductoTerminado);
        result.Value.TotalItems.ShouldBe(2);
        session!.Conteos.Count.ShouldBe(2);
        foreach (var producto in productos)
        {
            var conteo = session.Conteos.Single(c => c.ProductoTerminadoId == producto.Id);
            conteo.CantidadSistema.ShouldBe(producto.StockActual);
            conteo.CantidadContada.ShouldBe(producto.StockActual);
            conteo.ConteoOk.ShouldBeTrue();
        }
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_TipoInvalido_ReturnsValidationErrorSinEscrituras()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateInventarioSesionCommand((TipoInventario)99, Guid.NewGuid(), null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("TipoInventario");
        result.Error.Message.ShouldContain("Tipo de inventario no válido");
        await _inventarioSesionRepository.DidNotReceive().AddAsync(Arg.Any<InventarioSesion>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarConteo_ActualizaCantidadYRecomputaDiferenciaYConteoOk()
    {
        var conteo = Conteo(insumoId: Guid.NewGuid(), null, sistema: 10m, contada: 10m);
        var session = CrearSesion(new byte[] { 1 }, conteo);
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateRegistrarHandler().HandleAsync(
            new RegistrarConteoCommand(session.Id, conteo.Id, 14m, "sobra"));

        result.IsSuccess.ShouldBeTrue();
        conteo.CantidadContada.ShouldBe(14m);
        conteo.Observaciones.ShouldBe("sobra");
        result.Value.Diferencia.ShouldBe(4m);
        result.Value.ConteoOk.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarConteo_SesionCerrada_ReturnsSesionCerrada()
    {
        var conteo = Conteo(Guid.NewGuid(), null, 10m, 10m);
        var session = CrearSesion(new byte[] { 1 }, conteo);
        session.Estado = EstadoInventario.Cerrada;
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateRegistrarHandler().HandleAsync(
            new RegistrarConteoCommand(session.Id, conteo.Id, 12m, null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("SESION_CERRADA");
        conteo.CantidadContada.ShouldBe(10m);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarConteo_ConteoNoEncontrado_ReturnsConteoNotFound()
    {
        var session = CrearSesion(new byte[] { 1 }, Conteo(Guid.NewGuid(), null, 10m, 10m));
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateRegistrarHandler().HandleAsync(
            new RegistrarConteoCommand(session.Id, Guid.NewGuid(), 12m, null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("CONTEO_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_CierreConDiferencias_ActualizaStockCreaAjustesYCierraAtomico()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var insumo = CrearInsumo(stock: 10m);
        var pt = CrearProducto(stock: 10m);
        var session = CrearSesion(rowVersion,
            Conteo(insumo.Id, null, sistema: 10m, contada: 14m),   // +4 → AjustePositivo
            Conteo(null, pt.Id, sistema: 10m, contada: 7m));       // -3 → AjusteNegativo
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { insumo });
        _productoTerminadoRepository.GetTrackedByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { pt });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        var movimientos = new List<MovimientoStock>();
        _movimientoStockRepository.When(r => r.AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>()))
            .Do(ci => movimientos.Add(ci.Arg<MovimientoStock>()));

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmInventarioSesionCommand(session.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoInventario.Cerrada);
        result.Value.AjustesGenerados.ShouldBe(2);
        result.Value.DiferenciaTotal.ShouldBe(7m); // 4 + 3
        session.Estado.ShouldBe(EstadoInventario.Cerrada);
        insumo.StockActual.ShouldBe(14m);
        pt.StockActual.ShouldBe(7m);

        movimientos.Count.ShouldBe(2);
        var positivo = movimientos.Single(m => m.Tipo == TipoMovimientoStock.AjustePositivo);
        positivo.InsumoId.ShouldBe(insumo.Id);
        positivo.Cantidad.ShouldBe(4m);
        positivo.DocumentoOrigen.ShouldBe(session.Id.ToString());
        var negativo = movimientos.Single(m => m.Tipo == TipoMovimientoStock.AjusteNegativo);
        negativo.ProductoTerminadoId.ShouldBe(pt.Id);
        negativo.Cantidad.ShouldBe(-3m);
        negativo.DocumentoOrigen.ShouldBe(session.Id.ToString());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_CantidadContadaNegativa_ReturnsCantidadNegativaSinEscrituras()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var insumo = CrearInsumo(stock: 10m);
        var session = CrearSesion(rowVersion, Conteo(insumo.Id, null, sistema: 10m, contada: -2m));
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmInventarioSesionCommand(session.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("CANTIDAD_NEGATIVA");
        session.Estado.ShouldBe(EstadoInventario.Abierta);
        await _insumoRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_SesionCerrada_ReturnsSesionNoConfirmable()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var session = CrearSesion(rowVersion);
        session.Estado = EstadoInventario.Cerrada;
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmInventarioSesionCommand(session.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("SESION_NO_CONFIRMABLE");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_RowVersionDistinta_ReturnsConcurrency()
    {
        var session = CrearSesion(new byte[] { 1, 2, 3 });
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateConfirmHandler().HandleAsync(
            new ConfirmInventarioSesionCommand(session.Id, new byte[] { 9, 9, 9 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var insumo = CrearInsumo(stock: 10m);
        var session = CrearSesion(rowVersion, Conteo(insumo.Id, null, sistema: 10m, contada: 14m));
        _inventarioSesionRepository.GetByIdWithConteosAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { insumo });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmInventarioSesionCommand(session.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}
