using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;
using RecetaEntity = CentroDeProduccion.Domain.Entities.Receta;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the validated "Producción simple (1 receta = 1 PT)" flow: creation seeds
/// consumption lines from the recipe BOM (sub-recipes included, converted units), editing
/// replaces them freely while Borrador, and confirmation deducts the EDITED quantities,
/// find-or-creates the finished product derived from the recipe name, and costs by real
/// consumption ÷ declared output.
/// </summary>
public class ProduccionSimpleCommandHandlerTests
{
    private readonly IProduccionRepository _produccionRepository = Substitute.For<IProduccionRepository>();
    private readonly IRecetaRepository _recetaRepository = Substitute.For<IRecetaRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IMovimientoStockRepository _movimientoStockRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly IUnidadMedidaRepository _unidadMedidaRepository = Substitute.For<IUnidadMedidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly UnidadMedida UnidadBase = new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Unidad",
        Simbolo = "Uni"
    };

    private static Insumo CreateInsumo(string nombre, Guid unidadConsumoId, Guid unidadCompraId, decimal stock = 100m, decimal precio = 50m)
        => new()
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            CodigoSku = $"SKU-{nombre}",
            CategoriaId = Guid.NewGuid(),
            UnidadCompraId = unidadCompraId,
            UnidadConsumoId = unidadConsumoId,
            FactorConversion = 1,
            StockActual = stock,
            Activo = true,
            PrecioUltimaCompra = precio
        };

    // ── CreateProduccion: seeds InsumosConsumidos from the BOM ─────────────────────────────

    [Fact]
    public async Task Create_SeedsConsumptionLinesFromBomIncludingSubRecipesAndConvertedUnits()
    {
        var unidadKg = Guid.NewGuid();
        var unidadG = Guid.NewGuid();

        // Harina declared in kg (purchase unit), consumed in g (×1000 conversion)
        var harina = new Insumo
        {
            Id = Guid.NewGuid(), Nombre = "Harina", CodigoSku = "HAR-001",
            CategoriaId = Guid.NewGuid(),
            UnidadCompraId = unidadKg, UnidadConsumoId = unidadG, FactorConversion = 1000m,
            StockActual = 5000m, Activo = true, PrecioUltimaCompra = 20m
        };
        var agua = CreateInsumo("Agua", Guid.NewGuid(), Guid.NewGuid(), precio: 0m);

        // Sub-recipe: each Masa batch needs 4 Agua + 1 Kg harina; main recipe uses 3 Masa batches
        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA" };
        masa.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = masa.Id, InsumoId = agua.Id, CantidadNecesaria = 4, UnidadMedidaId = agua.UnidadConsumoId });
        masa.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = masa.Id, InsumoId = harina.Id, CantidadNecesaria = 1, UnidadMedidaId = unidadKg });

        var pan = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Pan francés", CodigoSku = "REC-PAN" };
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, RecetaOrigenId = masa.Id, CantidadNecesaria = 3, UnidadMedidaId = agua.UnidadConsumoId });

        _recetaRepository.GetByIdWithDetallesAsync(pan.Id, Arg.Any<CancellationToken>()).Returns(pan);
        _recetaRepository.GetByIdWithDetallesAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina, agua });

        var result = await new CreateProduccionCommandHandler(
            _produccionRepository, _recetaRepository, _insumoRepository, _unitOfWork, _currentUser,
            new CreateProduccionCommandValidator()).HandleAsync(new CreateProduccionCommand(pan.Id, null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoProduccion.Borrador);

        await _produccionRepository.Received(1).AddAsync(
            Arg.Do<Produccion>(p =>
            {
                p.InsumosConsumidos.Count.ShouldBe(2);
                p.Salidas.Count.ShouldBe(0); // no salidas created at draft time anymore

                // Harina: 1 kg per Masa batch × 3 batches, flattened to consumption
                // units (1kg → 1000g conversion) = 3000 g
                p.InsumosConsumidos.First(l => l.InsumoId == harina.Id).Cantidad.ShouldBe(3000m);
                // Agua: 4 per Masa batch × 3 batches = 12
                p.InsumosConsumidos.First(l => l.InsumoId == agua.Id).Cantidad.ShouldBe(12m);
            }), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_RecetaNotFound_ReturnsNotFoundError()
    {
        _recetaRepository.GetByIdWithDetallesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((RecetaEntity?)null);

        var result = await new CreateProduccionCommandHandler(
            _produccionRepository, _recetaRepository, _insumoRepository, _unitOfWork, _currentUser,
            new CreateProduccionCommandValidator()).HandleAsync(new CreateProduccionCommand(Guid.NewGuid(), null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("RECETA_NOT_FOUND");
    }

    // ── EditarInsumos: full replace + Borrador guard ───────────────────────────────────────

    private static Produccion CreateProduccionBorrador(byte[] rowVersion)
        => new()
        {
            Id = Guid.NewGuid(),
            RecetaId = Guid.NewGuid(),
            Fecha = DateTime.UtcNow,
            ResponsableId = Guid.NewGuid(),
            Estado = EstadoProduccion.Borrador,
            RowVersion = rowVersion,
            InsumosConsumidos =
            {
                new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = Guid.NewGuid(), Cantidad = 5m }
            }
        };

    [Fact]
    public async Task EditarInsumos_HappyPath_ReplacesFullList()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var produccion = CreateProduccionBorrador(rowVersion);
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var nuevoInsumo = CreateInsumo("Levadura", Guid.NewGuid(), Guid.NewGuid());
        var inactivo = CreateInsumo("Viejo", Guid.NewGuid(), Guid.NewGuid());
        inactivo.Activo = false;
        _insumoRepository.GetByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(nuevoInsumo.Id) && !ids.Contains(inactivo.Id)),
                Arg.Any<CancellationToken>())
            .Returns(new[] { nuevoInsumo });

        var result = await new EditarInsumosProduccionCommandHandler(
            _produccionRepository, _insumoRepository, _unitOfWork,
            new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(nuevoInsumo.Id, 25m, "extra por prueba")
            }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.CantidadLineas.ShouldBe(1);
        produccion.InsumosConsumidos.ShouldHaveSingleItem();
        var linea = produccion.InsumosConsumidos.Single();
        linea.InsumoId.ShouldBe(nuevoInsumo.Id);   // old template line replaced
        linea.Cantidad.ShouldBe(25m);              // edited quantity, NOT template's 5
        linea.Observaciones.ShouldBe("extra por prueba");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarInsumos_NoBorradorGuard_ReturnsConflictError()
    {
        var produccion = CreateProduccionBorrador(new byte[] { 1 });
        produccion.Estado = EstadoProduccion.Confirmada;
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await new EditarInsumosProduccionCommandHandler(
            _produccionRepository, _insumoRepository, _unitOfWork,
            new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(Guid.NewGuid(), 1m, null)
            }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PRODUCCION_NO_EDITABLE");
        await _insumoRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditarInsumos_InsumoNotFoundOrInactive_ReturnsNotFoundError()
    {
        var produccion = CreateProduccionBorrador(new byte[] { 1 });
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var missingId = Guid.NewGuid();
        _insumoRepository.GetByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(missingId)), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Insumo>());

        var result = await new EditarInsumosProduccionCommandHandler(
            _produccionRepository, _insumoRepository, _unitOfWork,
            new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(missingId, 2m, null)
            }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUMO_NOT_FOUND");
    }

    // ── Confirm: deducts EDITED quantities, find-or-create PT, lot + P.U., guards ──────────

    private static Produccion CreateEditableProduccion(byte[] rowVersion, params ProduccionInsumo[] lineas)
        => new()
        {
            Id = Guid.NewGuid(),
            RecetaId = Guid.NewGuid(),
            Estado = EstadoProduccion.Borrador,
            RowVersion = rowVersion,
            InsumosConsumidos = new List<ProduccionInsumo>(lineas)
        };

    private ConfirmProduccionCommandHandler CreateConfirmHandler() => new(
        _produccionRepository, _recetaRepository, _insumoRepository, _productoTerminadoRepository,
        _movimientoStockRepository, _unidadMedidaRepository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Confirm_ConsumesEditedQuantities_NotTemplate_And_CreatesPTDerivedFromReceta()
    {
        var rowVersion = new byte[] { 7, 7, 7 };
        var harina = CreateInsumo("Harina", Guid.NewGuid(), Guid.NewGuid(), stock: 3000m, precio: 10m);
        var levadura = CreateInsumo("Levadura", Guid.NewGuid(), Guid.NewGuid(), stock: 100m, precio: 25m);

        // Template seeded 5 of harina, but operator edited to 2500 and added levadura.
        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = harina.Id, Cantidad = 2500m },
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = levadura.Id, Cantidad = 30m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina, levadura });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _unidadMedidaRepository.GetByNombreAsync("Unidad", Arg.Any<CancellationToken>()).Returns(UnidadBase);

        var receta = new RecetaEntity
        {
            Id = produccion.RecetaId, Nombre = "Pan de Molde Nuevo", CodigoSku = "REC-MOLDE",
            CategoriaId = Guid.NewGuid()
        };
        _recetaRepository.GetByIdAsync(produccion.RecetaId, Arg.Any<CancellationToken>()).Returns(receta);
        _productoTerminadoRepository.GetByNombreAsync("Pan de Molde Nuevo", Arg.Any<CancellationToken>())
            .Returns((ProductoTerminado?)null); // not found → CREATE branch

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 40m, rowVersion));

        result.IsSuccess.ShouldBeTrue();

        // Stock deducted with the EDITED quantities, not the template values.
        harina.StockActual.ShouldBe(3000m - 2500m);
        levadura.StockActual.ShouldBe(100m - 30m);

        // PT created derived from the recipe fields.
        await _productoTerminadoRepository.Received(1).AddAsync(
            Arg.Do<ProductoTerminado>(pt =>
            {
                pt.Nombre.ShouldBe("Pan de Molde Nuevo");
                pt.CategoriaId.ShouldBe(receta.CategoriaId);
                pt.UnidadMedidaId.ShouldBe(UnidadBase.Id);
                pt.StockActual.ShouldBe(0m);
                pt.Activo.ShouldBeTrue();
                pt.CodigoSku.StartsWith("PT-").ShouldBeTrue();
            }), Arg.Any<CancellationToken>());

        // One internal salida row for report compatibility.
        produccion.Salidas.ShouldHaveSingleItem();
        var salida = produccion.Salidas.Single();
        salida.TipoSalida.ShouldBe(TipoSalidaProduccion.Primario);
        salida.Cantidad.ShouldBe(40m);
        // Costeo: Σ reales = 2500×10 + 30×25 = 25750
        produccion.CostoTotalInsumos.ShouldBe(25750m);
        produccion.CantidadProducida.ShouldBe(40m);
        produccion.Estado.ShouldBe(EstadoProduccion.Confirmada);
        produccion.Lote.ShouldStartWith(receta.CodigoSku + "-");
        result.Value.ProductoTerminadoId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Confirm_FindsExistingPT_UsesItInsteadOfCreating()
    {
        var rowVersion = new byte[] { 9 };
        var harina = CreateInsumo("Harina", Guid.NewGuid(), Guid.NewGuid(), stock: 100m, precio: 2m);

        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = harina.Id, Cantidad = 10m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var receta = new RecetaEntity
        {
            Id = produccion.RecetaId, Nombre = "Pan Existente", CodigoSku = "REC-EXI",
            CategoriaId = Guid.NewGuid()
        };
        _recetaRepository.GetByIdAsync(produccion.RecetaId, Arg.Any<CancellationToken>()).Returns(receta);

        var existente = new ProductoTerminado        {
            Id = Guid.NewGuid(), Nombre = "Pan Existente", CodigoSku = "PT-EXISTENTE",
            CategoriaId = Guid.NewGuid(), UnidadMedidaId = Guid.NewGuid(),
            StockActual = 7m, Activo = true
        };
        _productoTerminadoRepository.GetByNombreAsync("Pan Existente", Arg.Any<CancellationToken>())
            .Returns(existente); // found → USE branch

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 5m, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ProductoTerminadoId.ShouldBe(existente.Id);
        existente.StockActual.ShouldBe(12m); // 7 + 5
        await _productoTerminadoRepository.DidNotReceive().AddAsync(Arg.Any<ProductoTerminado>(), Arg.Any<CancellationToken>());
        produccion.Salidas.Single().ProductoTerminadoId.ShouldBe(existente.Id);
    }

    [Fact]
    public async Task Confirm_InsufficientStock_StillSucceeds_AndStockGoesNegative()
    {
        var rowVersion = new byte[] { 3 };
        var harina = CreateInsumo("Harina", Guid.NewGuid(), Guid.NewGuid(), stock: 10m, precio: 1m);

        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = harina.Id, Cantidad = 50m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _recetaRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new RecetaEntity { Id = produccion.RecetaId, Nombre = "R", CodigoSku = "SKU" });
        _unidadMedidaRepository.GetByNombreAsync("Unidad", Arg.Any<CancellationToken>()).Returns(UnidadBase);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 5m, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        harina.StockActual.ShouldBe(10m - 50m); // negative stock allowed
        produccion.Estado.ShouldBe(EstadoProduccion.Confirmada);
        // One ConsumoProduccion movement + one finished-product Produccion movement.
        await _movimientoStockRepository.Received(2).AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_RowVersionMismatch_ReturnsConcurrencyError()
    {
        var produccion = CreateEditableProduccion(new byte[] { 1 },
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = Guid.NewGuid(), Cantidad = 1m });
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 5m, new byte[] { 2 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Confirm_ZeroCantidadProducida_ReturnsValidationError()
    {
        var rowVersion = new byte[] { 4 };
        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = Guid.NewGuid(), Cantidad = 1m });
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 0m, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CANTIDAD_PRODUCIDA_INVALIDA");
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmed_ReturnsConflictError()
    {
        var rowVersion = new byte[] { 5 };
        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), InsumoId = Guid.NewGuid(), Cantidad = 1m });
        produccion.Estado = EstadoProduccion.Confirmada;
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 5m, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PRODUCCION_YA_CONFIRMADA");
    }
}
