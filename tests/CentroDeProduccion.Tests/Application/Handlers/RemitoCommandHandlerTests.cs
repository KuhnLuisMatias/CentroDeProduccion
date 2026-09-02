using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Remitos.Commands.CancelarRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateEstadoRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateRemito;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies remito creation: the Pendiente state, the PT-line price snapshot (recipe BOM cost
/// computed on the fly) and the insumo-line price snapshot (PAP marked up by the bar's resale
/// margin), plus the active-bar and non-empty-lines guards.
/// </summary>
public class CreateRemitoCommandHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<CreateRemitoCommand> _validator = new CreateRemitoCommandValidator();

    private ProductoTerminadoCostoResolver _costoResolver = CrearCostoResolver(null).Resolver;

    private CreateRemitoCommandHandler CreateHandler() => new(
        _remitoRepository, _barRepository, _productoTerminadoRepository, _insumoRepository,
        _costoResolver, _unitOfWork, _currentUser, _validator);

    private static Bar CrearBar(bool activo = true, decimal margen = 0m) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        MargenReventaPorcentaje = margen,
        Estado = activo ? EstadoBar.Activo : EstadoBar.Inactivo
    };

    /// <summary>Builds a resolver whose receta (id returned in the tuple) has one line of an
    /// insumo priced <paramref name="costoUnitario"/> at quantity 1 → BOM cost = costoUnitario.
    /// costoUnitario null → resolver always returns 0 (product without recipe).</summary>
    internal static (ProductoTerminadoCostoResolver Resolver, Guid? RecetaId) CrearCostoResolver(decimal? costoUnitario)
    {
        var recetaRepo = Substitute.For<IRecetaRepository>();        var insumoRepo = Substitute.For<IInsumoRepository>();
        if (costoUnitario.HasValue)
        {
            var insumo = new Insumo
            {
                Id = Guid.NewGuid(),
                Nombre = "Harina",
                CodigoSku = "HAR-001",
                PrecioUltimaCompra = costoUnitario.Value,
                Activo = true
            };
            var receta = new Receta
            {
                Id = Guid.NewGuid(),
                Nombre = "Pan Rústico",
                Insumos = new List<RecetaInsumo> { new() { InsumoId = insumo.Id, CantidadNecesaria = 1m } }
            };
            recetaRepo.GetByIdWithDetallesAsync(receta.Id, Arg.Any<CancellationToken>()).Returns(receta);
            insumoRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new[] { insumo });
            return (new ProductoTerminadoCostoResolver(recetaRepo, new RecetaCostoResolver(recetaRepo, insumoRepo)), receta.Id);
        }

        return (new ProductoTerminadoCostoResolver(recetaRepo, new RecetaCostoResolver(recetaRepo, insumoRepo)), null);
    }

    private ProductoTerminado CrearProducto(decimal costoUnitario)
    {
        var (resolver, recetaId) = CrearCostoResolver(costoUnitario);
        _costoResolver = resolver;
        return new ProductoTerminado
        {
            Id = Guid.NewGuid(),
            Nombre = "Pan Rústico",
            CodigoSku = "PAN-001",
            RecetaId = recetaId,
            StockActual = 100m
        };
    }

    private static Insumo CrearInsumo(decimal precioPromedio) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina",
        CodigoSku = "HAR-001",
        PrecioUltimaCompra = precioPromedio,
        StockActual = 100m,
        Activo = true
    };

    [Fact]
    public async Task HandleAsync_LineaProductoTerminado_PrecioEsCostoUnitario()
    {
        var bar = CrearBar();
        var producto = CrearProducto(100m);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _productoTerminadoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });
        _remitoRepository.GetNextNumeroAsync().Returns(7);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        Remito? creado = null;
        _remitoRepository.When(r => r.AddAsync(Arg.Any<Remito>(), Arg.Any<CancellationToken>()))
            .Do(ci => creado = ci.Arg<Remito>());

        var result = await CreateHandler().HandleAsync(new CreateRemitoCommand(bar.Id, null, null, null, new[]
        {
            new CreateRemitoLineaCommand(TipoLineaRemito.ProductoTerminado, producto.Id, null, 10m, null, null)
        }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.NumeroRemito.ShouldBe(7);
        result.Value.Estado.ShouldBe(EstadoRemito.Pendiente);
        result.Value.Total.ShouldBe(1000m); // 10 × 100
        creado.ShouldNotBeNull();
        creado!.BarId.ShouldBe(bar.Id);
        creado.Estado.ShouldBe(EstadoRemito.Pendiente);
        var linea = creado.Lineas.ShouldHaveSingleItem();
        linea.TipoLinea.ShouldBe(TipoLineaRemito.ProductoTerminado);
        linea.PrecioUnitario.ShouldBe(100m);
        linea.Subtotal.ShouldBe(1000m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LineaInsumoMargenCero_PrecioEsPAP()
    {
        var bar = CrearBar(margen: 0m);
        var insumo = CrearInsumo(50m);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _remitoRepository.GetNextNumeroAsync().Returns(1);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        Remito? creado = null;
        _remitoRepository.When(r => r.AddAsync(Arg.Any<Remito>(), Arg.Any<CancellationToken>()))
            .Do(ci => creado = ci.Arg<Remito>());

        var result = await CreateHandler().HandleAsync(new CreateRemitoCommand(bar.Id, null, null, null, new[]
        {
            new CreateRemitoLineaCommand(TipoLineaRemito.Insumo, null, insumo.Id, 10m, null, null)
        }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Total.ShouldBe(500m); // 10 × 50
        var linea = creado!.Lineas.ShouldHaveSingleItem();
        linea.PrecioUnitario.ShouldBe(50m);
        linea.Subtotal.ShouldBe(500m);
    }

    [Fact]
    public async Task HandleAsync_LineaInsumoConMargen_AplicaMargenRedondeado()
    {
        var bar = CrearBar(margen: 15m);
        var insumo = CrearInsumo(33.3333m);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _remitoRepository.GetNextNumeroAsync().Returns(1);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        Remito? creado = null;
        _remitoRepository.When(r => r.AddAsync(Arg.Any<Remito>(), Arg.Any<CancellationToken>()))
            .Do(ci => creado = ci.Arg<Remito>());

        var result = await CreateHandler().HandleAsync(new CreateRemitoCommand(bar.Id, null, null, null, new[]
        {
            new CreateRemitoLineaCommand(TipoLineaRemito.Insumo, null, insumo.Id, 2m, null, null)
        }));

        result.IsSuccess.ShouldBeTrue();
        var linea = creado!.Lineas.ShouldHaveSingleItem();
        linea.PrecioUnitario.ShouldBe(38.3333m); // round(33.3333 × 1.15, 4) = 38.3333
        linea.Subtotal.ShouldBe(76.6666m); // 2 × 38.3333
        result.Value.Total.ShouldBe(76.6666m);
    }

    [Fact]
    public async Task HandleAsync_BarInactivo_ReturnsBarInactivo()
    {
        var bar = CrearBar(activo: false);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(new CreateRemitoCommand(bar.Id, null, null, null, new[]
        {
            new CreateRemitoLineaCommand(TipoLineaRemito.ProductoTerminado, Guid.NewGuid(), null, 1m, null, null)
        }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("BAR_INACTIVO");
        await _remitoRepository.DidNotReceive().AddAsync(Arg.Any<Remito>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SinLineas_ReturnsValidationError()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateRemitoCommand(Guid.NewGuid(), null, null, null, Array.Empty<CreateRemitoLineaCommand>()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("al menos una línea");
        await _remitoRepository.DidNotReceive().AddAsync(Arg.Any<Remito>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies remito edits: Pendiente remitos accept a full header update with replaced and
/// re-priced lines, while Enviado remitos are rejected as REMITO_NO_EDITABLE.
/// </summary>
public class UpdateRemitoCommandHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<UpdateRemitoCommand> _validator = new UpdateRemitoCommandValidator();

    private ProductoTerminadoCostoResolver _costoResolver =
        CreateRemitoCommandHandlerTests.CrearCostoResolver(null).Resolver;

    private UpdateRemitoCommandHandler CreateHandler() => new(
        _remitoRepository, _barRepository, _productoTerminadoRepository, _insumoRepository,
        _costoResolver, _unitOfWork, _validator);

    private static Bar CrearBar() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        MargenReventaPorcentaje = 10m,
        Estado = EstadoBar.Activo
    };

    private ProductoTerminado CrearProducto(decimal costoUnitario)
    {
        var (resolver, recetaId) = CreateRemitoCommandHandlerTests.CrearCostoResolver(costoUnitario);
        _costoResolver = resolver;
        return new ProductoTerminado
        {
            Id = Guid.NewGuid(),
            Nombre = "Pan Rústico",
            CodigoSku = "PAN-001",
            RecetaId = recetaId,
            StockActual = 100m
        };
    }

    private static Remito CrearRemito(EstadoRemito estado, byte[] rowVersion) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 3,
        BarId = Guid.NewGuid(),
        Estado = estado,
        RowVersion = rowVersion,
        Lineas = new List<RemitoLinea>()
    };

    [Fact]
    public async Task HandleAsync_RemitoPendiente_ActualizaCabeceraYReemplazaLineasRepreciadas()
    {
        var bar = CrearBar();
        var producto = CrearProducto(100m);
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion);
        remito.Lineas.Add(new RemitoLinea
        {
            Id = Guid.NewGuid(),
            TipoLinea = TipoLineaRemito.ProductoTerminado,
            ProductoTerminadoId = producto.Id,
            Cantidad = 1m,
            PrecioUnitario = 999m,
            Subtotal = 999m
        });
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _productoTerminadoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { producto });

        var result = await CreateHandler().HandleAsync(new UpdateRemitoCommand(
            remito.Id, bar.Id, "Edición", "Juan", "Pedro", new[]
            {
                new CreateRemitoLineaCommand(TipoLineaRemito.ProductoTerminado, producto.Id, null, 10m, "L-1", null)
            }, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        remito.BarId.ShouldBe(bar.Id);
        remito.Observaciones.ShouldBe("Edición");
        remito.EntregadoPor.ShouldBe("Juan");
        remito.RecibidoPor.ShouldBe("Pedro");
        var linea = remito.Lineas.ShouldHaveSingleItem();
        linea.Cantidad.ShouldBe(10m);
        linea.PrecioUnitario.ShouldBe(100m); // re-priced at CostoUnitario
        linea.Subtotal.ShouldBe(1000m);
        linea.Lote.ShouldBe("L-1");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RemitoEnviado_ReturnsNoEditable()
    {
        var bar = CrearBar();
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Enviado, rowVersion);
        _remitoRepository.GetByIdWithLineasAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new UpdateRemitoCommand(
            remito.Id, bar.Id, null, null, null, new[]
            {
            new CreateRemitoLineaCommand(TipoLineaRemito.ProductoTerminado, Guid.NewGuid(), null, 1m, null, null)
            }, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_NO_EDITABLE");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies the Pendiente ↔ EnProceso transitions; any transition involving Enviado or
/// Cancelado is rejected as ESTADO_TRANSICION_INVALIDA.
/// </summary>
public class UpdateEstadoRemitoCommandHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateEstadoRemitoCommandHandler CreateHandler() => new(_remitoRepository, _unitOfWork);

    private static Remito CrearRemito(EstadoRemito estado, byte[] rowVersion) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 5,
        Estado = estado,
        RowVersion = rowVersion
    };

    [Fact]
    public async Task HandleAsync_PendienteAEnProceso_Transiciona()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion);
        _remitoRepository.GetByIdAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new UpdateEstadoRemitoCommand(remito.Id, EstadoRemito.EnProceso, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        remito.Estado.ShouldBe(EstadoRemito.EnProceso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EnviadoACualquierEstado_ReturnsTransicionInvalida()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Enviado, rowVersion);
        _remitoRepository.GetByIdAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new UpdateEstadoRemitoCommand(remito.Id, EstadoRemito.EnProceso, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("ESTADO_TRANSICION_INVALIDA");
        remito.Estado.ShouldBe(EstadoRemito.Enviado);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies cancellation: Pendiente/EnProceso remitos cancel without touching stock, while
/// Enviado remitos are rejected as REMITO_NO_CANCELABLE.
/// </summary>
public class CancelarRemitoCommandHandlerTests
{
    private readonly IRemitoRepository _remitoRepository = Substitute.For<IRemitoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CancelarRemitoCommandHandler CreateHandler() => new(_remitoRepository, _unitOfWork);

    private static Remito CrearRemito(EstadoRemito estado, byte[] rowVersion) => new()
    {
        Id = Guid.NewGuid(),
        NumeroRemito = 9,
        Estado = estado,
        RowVersion = rowVersion,
        Lineas = new List<RemitoLinea>
        {
            new() { Id = Guid.NewGuid(), TipoLinea = TipoLineaRemito.ProductoTerminado, Cantidad = 10m, Subtotal = 1000m }
        }
    };

    [Fact]
    public async Task HandleAsync_Pendiente_CancelaSinTocarStock()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Pendiente, rowVersion);
        _remitoRepository.GetByIdAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new CancelarRemitoCommand(remito.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        remito.Estado.ShouldBe(EstadoRemito.Cancelado);
        remito.Lineas.Single().Subtotal.ShouldBe(1000m); // no stock side effect
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Enviado_ReturnsNoCancelable()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var remito = CrearRemito(EstadoRemito.Enviado, rowVersion);
        _remitoRepository.GetByIdAsync(remito.Id).Returns(remito);

        var result = await CreateHandler().HandleAsync(new CancelarRemitoCommand(remito.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("REMITO_NO_CANCELABLE");
        remito.Estado.ShouldBe(EstadoRemito.Enviado);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}