using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;
using RecetaEntity = CentroDeProduccion.Domain.Entities.Receta;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the validated "Producción simple (1 receta = 1 PT)" flow: creation seeds
/// consumption lines from the recipe's OWN BOM (single level: insumo lines converted to
/// consumption units + sub-recipe lines kept as RecetaOrigenId), editing replaces them freely
/// while Borrador, and confirmation deducts the EDITED quantities — insumos from insumo stock,
/// sub-recipe lines from the active finished product of that sub-recipe (fail-fast when it is
/// missing or short on stock) —, find-or-creates the finished product derived from the recipe
/// name, and costs by real consumption ÷ declared output.
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

    private ProductoTerminadoCostoResolver CreateCostoResolver()
        => new(_recetaRepository, new RecetaCostoResolver(_recetaRepository, _insumoRepository));

    // ── CreateProduccion: seeds InsumosConsumidos from the recipe's own BOM ────────────────

    [Fact]
    public async Task Create_SeedsLinesFromOwnBom_SubRecetaKeptAsLineAndUnitsConverted()
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

        // Main recipe: 1 kg harina (direct, converted to g) + 3 batches of the Masa sub-recipe
        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA" };

        var pan = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Pan francés", CodigoSku = "REC-PAN" };
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, InsumoId = harina.Id, CantidadNecesaria = 1, UnidadMedidaId = unidadKg });
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, RecetaOrigenId = masa.Id, CantidadNecesaria = 3, UnidadMedidaId = UnidadBase.Id });

        _recetaRepository.GetByIdWithDetallesAsync(pan.Id, Arg.Any<CancellationToken>()).Returns(pan);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });

        var result = await new CreateProduccionCommandHandler(
            _produccionRepository, _recetaRepository, _insumoRepository, _unitOfWork, _currentUser,
            new CreateProduccionCommandValidator()).HandleAsync(new CreateProduccionCommand(pan.Id, null));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoProduccion.Borrador);

        await _produccionRepository.Received(1).AddAsync(
            Arg.Do<Produccion>(p =>
            {
                p.InsumosConsumidos.Count.ShouldBe(2);
                p.Salidas.Count.ShouldBe(0); // no salidas created at draft time

                // Harina: 1 kg per batch, converted to consumption units (1kg → 1000g)
                var lineaHarina = p.InsumosConsumidos.Single(l => l.InsumoId == harina.Id);
                lineaHarina.Cantidad.ShouldBe(1000m);
                lineaHarina.RecetaOrigenId.ShouldBeNull();

                // Sub-recipe survives as a PT-consumption line in the sub-recipe's result unit
                var lineaMasa = p.InsumosConsumidos.Single(l => l.RecetaOrigenId == masa.Id);
                lineaMasa.InsumoId.ShouldBeNull();
                lineaMasa.Cantidad.ShouldBe(3m);
            }), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_SelfReferencingSubReceta_ReturnsValidationError()
    {
        var pan = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Pan francés", CodigoSku = "REC-PAN" };
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, RecetaOrigenId = pan.Id, CantidadNecesaria = 1, UnidadMedidaId = UnidadBase.Id });

        _recetaRepository.GetByIdWithDetallesAsync(pan.Id, Arg.Any<CancellationToken>()).Returns(pan);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var result = await new CreateProduccionCommandHandler(
            _produccionRepository, _recetaRepository, _insumoRepository, _unitOfWork, _currentUser,
            new CreateProduccionCommandValidator()).HandleAsync(new CreateProduccionCommand(pan.Id, null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BOM_SELF_REFERENCE");
    }

    [Fact]
    public async Task Create_InvalidLineUnit_ReturnsBomUnitInvalidError()
    {
        var unidadLitro = Guid.NewGuid();
        var harina = new Insumo
        {
            Id = Guid.NewGuid(), Nombre = "Harina", CodigoSku = "HAR-001",
            CategoriaId = Guid.NewGuid(),
            UnidadCompraId = Guid.NewGuid(), UnidadConsumoId = Guid.NewGuid(), FactorConversion = 1m,
            StockActual = 100m, Activo = true, PrecioUltimaCompra = 10m
        };

        var pan = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Pan francés", CodigoSku = "REC-PAN" };
        pan.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = pan.Id, InsumoId = harina.Id, CantidadNecesaria = 1, UnidadMedidaId = unidadLitro });

        _recetaRepository.GetByIdWithDetallesAsync(pan.Id, Arg.Any<CancellationToken>()).Returns(pan);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });

        var result = await new CreateProduccionCommandHandler(
            _produccionRepository, _recetaRepository, _insumoRepository, _unitOfWork, _currentUser,
            new CreateProduccionCommandValidator()).HandleAsync(new CreateProduccionCommand(pan.Id, null));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BOM_UNIT_INVALID");
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
                _produccionRepository, _insumoRepository, _recetaRepository, _unitOfWork,
                new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(nuevoInsumo.Id, null, 25m, "extra por prueba")
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
    public async Task EditarInsumos_RecetaLine_ReplacesListWithSubRecetaConsumption()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var produccion = CreateProduccionBorrador(rowVersion);
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var subReceta = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA", Activo = true };
        _recetaRepository.GetByIdAsync(subReceta.Id, Arg.Any<CancellationToken>()).Returns(subReceta);

        var result = await new EditarInsumosProduccionCommandHandler(
                _produccionRepository, _insumoRepository, _recetaRepository, _unitOfWork,
                new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(null, subReceta.Id, 2m, null)
            }));

        result.IsSuccess.ShouldBeTrue();
        produccion.InsumosConsumidos.ShouldHaveSingleItem();
        var linea = produccion.InsumosConsumidos.Single();
        linea.InsumoId.ShouldBeNull();
        linea.RecetaOrigenId.ShouldBe(subReceta.Id);
        linea.Cantidad.ShouldBe(2m);
    }

    [Fact]
    public async Task EditarInsumos_NoBorradorGuard_ReturnsConflictError()
    {
        var produccion = CreateProduccionBorrador(new byte[] { 1 });
        produccion.Estado = EstadoProduccion.Confirmada;
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await new EditarInsumosProduccionCommandHandler(
                _produccionRepository, _insumoRepository, _recetaRepository, _unitOfWork,
                new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(Guid.NewGuid(), null, 1m, null)
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
                _produccionRepository, _insumoRepository, _recetaRepository, _unitOfWork,
                new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(missingId, null, 2m, null)
            }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUMO_NOT_FOUND");
    }

    [Fact]
    public async Task EditarInsumos_LineWithoutOrigin_ReturnsValidationError()
    {
        var produccion = CreateProduccionBorrador(new byte[] { 1 });
        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);

        var result = await new EditarInsumosProduccionCommandHandler(
                _produccionRepository, _insumoRepository, _recetaRepository, _unitOfWork,
                new EditarInsumosProduccionCommandValidator())
            .HandleAsync(new EditarInsumosProduccionCommand(produccion.Id, new[]
            {
                new LineaInsumoDto(null, null, 2m, null)
            }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
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
        _movimientoStockRepository, _unidadMedidaRepository, CreateCostoResolver(),
        _unitOfWork, _currentUser);

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
    public async Task Confirm_SubRecetaLine_DeductsSubPtStock_LedgersIt_AndAddsItsCost()
    {
        var rowVersion = new byte[] { 6 };
        var harina = CreateInsumo("Harina", Guid.NewGuid(), Guid.NewGuid(), stock: 100m, precio: 10m);

        // Sub-recipe "Masa": 2 harina per batch → live standard cost 2×10 = 20 per lote.
        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA", Activo = true };
        masa.Insumos.Add(new RecetaInsumo { Id = Guid.NewGuid(), RecetaId = masa.Id, InsumoId = harina.Id, CantidadNecesaria = 2, UnidadMedidaId = harina.UnidadConsumoId });

        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), RecetaOrigenId = masa.Id, Cantidad = 2m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _recetaRepository.GetByIdAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _recetaRepository.GetByIdWithDetallesAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { harina });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _unidadMedidaRepository.GetByNombreAsync("Unidad", Arg.Any<CancellationToken>()).Returns(UnidadBase);

        var subPt = new ProductoTerminado
        {
            Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "PT-MASA",
            CategoriaId = Guid.NewGuid(), UnidadMedidaId = UnidadBase.Id,
            RecetaId = masa.Id, StockActual = 5m, Activo = true
        };
        _productoTerminadoRepository.GetTrackedActiveByRecetaIdAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(subPt);

        var recetaPan = new RecetaEntity
        {
            Id = produccion.RecetaId, Nombre = "Pan de Molde", CodigoSku = "REC-MOLDE",
            CategoriaId = Guid.NewGuid()
        };
        _recetaRepository.GetByIdAsync(produccion.RecetaId, Arg.Any<CancellationToken>()).Returns(recetaPan);
        _productoTerminadoRepository.GetByNombreAsync("Pan de Molde", Arg.Any<CancellationToken>())
            .Returns(new ProductoTerminado
            {
                Id = Guid.NewGuid(), Nombre = "Pan de Molde", CodigoSku = "PT-MOLDE",
                CategoriaId = Guid.NewGuid(), UnidadMedidaId = UnidadBase.Id,
                StockActual = 0m, Activo = true
            });

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 10m, rowVersion));

        result.IsSuccess.ShouldBeTrue();

        // Sub-PT stock deducted by the line's quantity.
        subPt.StockActual.ShouldBe(5m - 2m);

        // Cost: 2 batches × live unit cost 20 = 40 added to the run's cost.
        produccion.CostoTotalInsumos.ShouldBe(40m);
        produccion.Estado.ShouldBe(EstadoProduccion.Confirmada);

        // Movements: 1 sub-PT outflow (VentaBar ledger type, like remito outflows)
        // + 1 finished-product inflow (Produccion).
        var movimientos = new List<MovimientoStock>();
        await _movimientoStockRepository.Received(2).AddAsync(
            Arg.Do<MovimientoStock>(m => movimientos.Add(m)), Arg.Any<CancellationToken>());

        var consumo = movimientos.Single(m => m.ProductoTerminadoId == subPt.Id);
        consumo.Tipo.ShouldBe(TipoMovimientoStock.VentaBar);
        consumo.Cantidad.ShouldBe(-2m);
        consumo.Motivo.ShouldStartWith("Consumo producción");
        movimientos.Single(m => m.Tipo == TipoMovimientoStock.Produccion).Cantidad.ShouldBe(10m);
    }

    [Fact]
    public async Task Confirm_SubRecetaWithoutActivePt_FailsConfirmation_WithoutMutating()
    {
        var rowVersion = new byte[] { 8 };
        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA", Activo = true };

        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), RecetaOrigenId = masa.Id, Cantidad = 2m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _recetaRepository.GetByIdAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _productoTerminadoRepository.GetTrackedActiveByRecetaIdAsync(masa.Id, Arg.Any<CancellationToken>())
            .Returns((ProductoTerminado?)null); // no active PT → FAIL
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _recetaRepository.GetByIdAsync(produccion.RecetaId, Arg.Any<CancellationToken>())
            .Returns(new RecetaEntity { Id = produccion.RecetaId, Nombre = "R", CodigoSku = "SKU" });

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 10m, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SUBRECETA_SIN_PT");
        result.Error.Message.ShouldContain("Prodúzcala primero");

        // Nothing mutated: still Borrador and no stock movement was written.
        produccion.Estado.ShouldBe(EstadoProduccion.Borrador);
        await _movimientoStockRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_SubRecetaWithInsufficientStock_FailsConfirmation()
    {
        var rowVersion = new byte[] { 11 };
        var masa = new RecetaEntity { Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "REC-MASA", Activo = true };

        var produccion = CreateEditableProduccion(rowVersion,
            new ProduccionInsumo { Id = Guid.NewGuid(), RecetaOrigenId = masa.Id, Cantidad = 4m });

        _produccionRepository.GetByIdWithSalidasAsync(produccion.Id, Arg.Any<CancellationToken>()).Returns(produccion);
        _recetaRepository.GetByIdAsync(masa.Id, Arg.Any<CancellationToken>()).Returns(masa);
        _productoTerminadoRepository.GetTrackedActiveByRecetaIdAsync(masa.Id, Arg.Any<CancellationToken>())
            .Returns(new ProductoTerminado
            {
                Id = Guid.NewGuid(), Nombre = "Masa base", CodigoSku = "PT-MASA",
                CategoriaId = Guid.NewGuid(), UnidadMedidaId = UnidadBase.Id,
                RecetaId = masa.Id, StockActual = 2m, Activo = true
            });
        _currentUser.UsuarioId.Returns(Guid.NewGuid());
        _recetaRepository.GetByIdAsync(produccion.RecetaId, Arg.Any<CancellationToken>())
            .Returns(new RecetaEntity { Id = produccion.RecetaId, Nombre = "R", CodigoSku = "SKU" });

        var result = await CreateConfirmHandler().HandleAsync(new ConfirmProduccionCommand(produccion.Id, 10m, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SUBRECETA_STOCK_INSUFICIENTE");
        produccion.Estado.ShouldBe(EstadoProduccion.Borrador);
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
