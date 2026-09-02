using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CancelarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.EnviarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the OrdenCompra creation flow: sequential Numero, Borrador state, creator from the
/// current user, and active-proveedor/insumo guards.
/// </summary>
public class CreateOrdenCompraCommandHandlerTests
{
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<CreateOrdenCompraCommand> _validator = new CreateOrdenCompraCommandValidator();

    private CreateOrdenCompraCommandHandler CreateHandler() => new(
        _ordenCompraRepository, _proveedorRepository, _insumoRepository, _unitOfWork, _currentUser, _validator);

    private static Proveedor CrearProveedor(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        NombreRazonSocial = "Distribuidora Sur",
        Cuit = "20-12345678-9",
        Activo = activo
    };

    private static Insumo CrearInsumo(bool activo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina",
        CodigoSku = "HAR-001",
        StockMinimo = 10,
        StockActual = 3,
        PrecioUltimaCompra = 100m,
        Activo = activo
    };

    private static CreateOrdenCompraCommand Command(Guid proveedorId, Guid insumoId) => new(
        proveedorId, null, new[]
        {
            new CreateOrdenCompraItemCommand(insumoId, 10m, 100m)
        });

    [Fact]
    public async Task HandleAsync_ProveedorEInsumoValidos_CreaBorradorConNumeroYCreadoPor()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo();
        var usuarioId = Guid.NewGuid();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _ordenCompraRepository.GetNextNumeroAsync().Returns(42);
        _currentUser.UsuarioId.Returns(usuarioId);

        OrdenCompra? creada = null;
        _ordenCompraRepository.When(r => r.AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>()))
            .Do(ci => creada = ci.Arg<OrdenCompra>());

        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, insumo.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Numero.ShouldBe(42);
        result.Value.Estado.ShouldBe(EstadoOrdenCompra.Borrador);
        result.Value.ProveedorId.ShouldBe(proveedor.Id);
        result.Value.Total.ShouldBe(1000m); // 10 × 100
        creada.ShouldNotBeNull();
        creada!.Estado.ShouldBe(EstadoOrdenCompra.Borrador);
        creada.CreadoPor.ShouldBe(usuarioId);
        creada.Numero.ShouldBe(42);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProveedorInactivo_ReturnsNotFound()
    {
        var proveedor = CrearProveedor(activo: false);
        var insumo = CrearInsumo();
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);

        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, insumo.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("PROVEEDOR_NOT_FOUND");
        await _ordenCompraRepository.DidNotReceive().AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SinItems_ReturnsValidationError()
    {
        var proveedor = CrearProveedor();

        var result = await CreateHandler().HandleAsync(
            new CreateOrdenCompraCommand(proveedor.Id, null, Array.Empty<CreateOrdenCompraItemCommand>()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _ordenCompraRepository.DidNotReceive().AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsumoInactivo_ReturnsNotFound()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo(activo: false);
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });

        var result = await CreateHandler().HandleAsync(Command(proveedor.Id, insumo.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUMO_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies order edits: only Borrador orders are editable, and the optimistic-concurrency guard
/// rejects stale writers with a CONCURRENCY_CONFLICT.
/// </summary>
public class UpdateOrdenCompraCommandHandlerTests
{
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly IProveedorRepository _proveedorRepository = Substitute.For<IProveedorRepository>();
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<UpdateOrdenCompraCommand> _validator = new UpdateOrdenCompraCommandValidator();

    private UpdateOrdenCompraCommandHandler CreateHandler() => new(
        _ordenCompraRepository, _proveedorRepository, _insumoRepository, _unitOfWork, _validator);

    private static Proveedor CrearProveedor() => new()
    {
        Id = Guid.NewGuid(),
        NombreRazonSocial = "Distribuidora Sur",
        Cuit = "20-12345678-9",
        Activo = true
    };

    private static Insumo CrearInsumo() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina",
        CodigoSku = "HAR-001",
        Activo = true
    };

    private static UpdateOrdenCompraCommand Command(Guid ordenId, Guid proveedorId, Guid insumoId, byte[] rowVersion) => new(
        ordenId, proveedorId, "Edición", rowVersion, new[]
        {
            new CreateOrdenCompraItemCommand(insumoId, 20m, 90m)
        });

    private static OrdenCompra CrearOrden(EstadoOrdenCompra estado, byte[] rowVersion) => new()
    {
        Id = Guid.NewGuid(),
        Numero = 5,
        Estado = estado,
        RowVersion = rowVersion,
        Items = new List<OrdenCompraItem>()
    };

    [Fact]
    public async Task HandleAsync_OrdenBorrador_ActualizaCabeceraYReemplazaItems()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo();
        var rowVersion = new byte[] { 1, 2, 3 };
        var orden = CrearOrden(EstadoOrdenCompra.Borrador, rowVersion);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });

        var result = await CreateHandler().HandleAsync(Command(orden.Id, proveedor.Id, insumo.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        orden.ProveedorId.ShouldBe(proveedor.Id);
        orden.Observaciones.ShouldBe("Edición");
        orden.Items.Count.ShouldBe(1);
        orden.Items.Single().InsumoId.ShouldBe(insumo.Id);
        orden.Items.Single().CantidadPedida.ShouldBe(20m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OrdenEnviada_ReturnsNoEditable()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo();
        var rowVersion = new byte[] { 1, 2, 3 };
        var orden = CrearOrden(EstadoOrdenCompra.Enviada, rowVersion);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(Command(orden.Id, proveedor.Id, insumo.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ORDEN_NO_EDITABLE");
        result.Error.Message.ShouldBe("Solo se pueden editar órdenes en estado Borrador");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var proveedor = CrearProveedor();
        var insumo = CrearInsumo();
        var rowVersion = new byte[] { 1, 2, 3 };
        var orden = CrearOrden(EstadoOrdenCompra.Borrador, rowVersion);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);
        _proveedorRepository.GetByIdAsync(proveedor.Id).Returns(proveedor);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateHandler().HandleAsync(Command(orden.Id, proveedor.Id, insumo.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}

/// <summary>
/// Verifies the Borrador → Enviada transition recording FechaEnvio, plus the state and
/// optimistic-concurrency guards.
/// </summary>
public class EnviarOrdenCompraCommandHandlerTests
{
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private EnviarOrdenCompraCommandHandler CreateHandler() => new(_ordenCompraRepository, _unitOfWork);

    private static OrdenCompra CrearOrden(EstadoOrdenCompra estado) => new()
    {
        Id = Guid.NewGuid(),
        Numero = 9,
        Estado = estado
    };

    [Fact]
    public async Task HandleAsync_OrdenBorrador_EnviaYRegistraFecha()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Borrador);
        _ordenCompraRepository.GetByIdAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(new EnviarOrdenCompraCommand(orden.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoOrdenCompra.Enviada);
        result.Value.Numero.ShouldBe(9);
        orden.FechaEnvio.ShouldNotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OrdenNoBorrador_ReturnsNoEnviable()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Enviada);
        _ordenCompraRepository.GetByIdAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(new EnviarOrdenCompraCommand(orden.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ORDEN_NO_ENVIABLE");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Borrador);
        _ordenCompraRepository.GetByIdAsync(orden.Id).Returns(orden);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateHandler().HandleAsync(new EnviarOrdenCompraCommand(orden.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}

/// <summary>
/// Verifies cancellation rules: Borrador and Enviada cancel freely, while other states
/// (Cancelada) return the proper error.
/// </summary>
public class CancelarOrdenCompraCommandHandlerTests
{
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CancelarOrdenCompraCommandHandler CreateHandler() => new(_ordenCompraRepository, _unitOfWork);

    private static OrdenCompra CrearOrden(EstadoOrdenCompra estado) => new()
    {
        Id = Guid.NewGuid(),
        Numero = 12,
        Estado = estado,
        Items = new List<OrdenCompraItem>()
    };

    [Fact]
    public async Task HandleAsync_OrdenBorrador_Cancela()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Borrador);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(new CancelarOrdenCompraCommand(orden.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoOrdenCompra.Cancelada);
        orden.Estado.ShouldBe(EstadoOrdenCompra.Cancelada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OrdenEnviada_Cancela()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Enviada);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(new CancelarOrdenCompraCommand(orden.Id));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoOrdenCompra.Cancelada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OrdenCancelada_ReturnsNoCancelable()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Cancelada);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);

        var result = await CreateHandler().HandleAsync(new CancelarOrdenCompraCommand(orden.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ORDEN_NO_CANCELABLE");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrenciaAlGuardar_ReturnsConcurrency()
    {
        var orden = CrearOrden(EstadoOrdenCompra.Borrador);
        _ordenCompraRepository.GetByIdWithItemsAsync(orden.Id).Returns(orden);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new ConcurrencyConflictException("conflicto", new Exception())));

        var result = await CreateHandler().HandleAsync(new CancelarOrdenCompraCommand(orden.Id));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}

/// <summary>
/// Verifies auto-generation of Borrador orders from stock alerts: one order per ProveedorPrincipal
/// with quantity = StockMinimo - StockActual (min 1) at the last purchase price.
/// </summary>
public class GenerarOCDesdeAlertasCommandHandlerTests
{
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IOrdenCompraRepository _ordenCompraRepository = Substitute.For<IOrdenCompraRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<GenerarOCDesdeAlertasCommand> _validator = new GenerarOCDesdeAlertasCommandValidator();

    private GenerarOCDesdeAlertasCommandHandler CreateHandler() => new(
        _insumoRepository, _ordenCompraRepository, _unitOfWork, _currentUser, _validator);

    private static Insumo CrearInsumo(
        Guid? proveedorId, decimal stockMinimo, decimal stockActual, decimal precioUltima = 0m) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Insumo",
        CodigoSku = "SKU-001",
        StockMinimo = stockMinimo,
        StockActual = stockActual,
        PrecioUltimaCompra = precioUltima,
        ProveedorPrincipalId = proveedorId,
        Activo = true
    };

    [Fact]
    public async Task HandleAsync_InsumosDeUnProveedor_CreaUnaOrdenConCantidadYPrecioCalculados()
    {
        var proveedorId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        // Cantidad: max(StockMinimo - StockActual, 1) = max(10-3, 1) = 7 y max(8-8, 1) = 1.
        var harina = CrearInsumo(proveedorId, stockMinimo: 10, stockActual: 3, precioUltima: 100m);
        var sal = CrearInsumo(proveedorId, stockMinimo: 8, stockActual: 8, precioUltima: 50m);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { harina, sal });
        _ordenCompraRepository.GetNextNumeroAsync().Returns(21);
        _currentUser.UsuarioId.Returns(usuarioId);

        var ordenes = new List<OrdenCompra>();
        _ordenCompraRepository.When(r => r.AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>()))
            .Do(ci => ordenes.Add(ci.Arg<OrdenCompra>()));

        var result = await CreateHandler().HandleAsync(new GenerarOCDesdeAlertasCommand(new[] { harina.Id, sal.Id }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ordenes.Count.ShouldBe(1);
        result.Value.Ordenes[0].CantidadItems.ShouldBe(2);
        result.Value.Ordenes[0].Estado.ShouldBe(EstadoOrdenCompra.Borrador);

        var creada = ordenes.ShouldHaveSingleItem();
        creada.ProveedorId.ShouldBe(proveedorId);
        creada.Numero.ShouldBe(21);
        creada.CreadoPor.ShouldBe(usuarioId);
        var itemHarina = creada.Items.Single(i => i.InsumoId == harina.Id);
        itemHarina.CantidadPedida.ShouldBe(7m);
        itemHarina.PrecioUnitario.ShouldBe(100m);
        var itemSal = creada.Items.Single(i => i.InsumoId == sal.Id);
        itemSal.CantidadPedida.ShouldBe(1m);
        itemSal.PrecioUnitario.ShouldBe(50m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SinInsumos_ReturnsValidationError()
    {
        var result = await CreateHandler().HandleAsync(new GenerarOCDesdeAlertasCommand(Array.Empty<Guid>()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _insumoRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await _ordenCompraRepository.DidNotReceive().AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsumoSinProveedorPrincipal_ReturnsError()
    {
        var insumo = CrearInsumo(proveedorId: null, stockMinimo: 10, stockActual: 3);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumo });

        var result = await CreateHandler().HandleAsync(new GenerarOCDesdeAlertasCommand(new[] { insumo.Id }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SIN_PROVEEDOR_PRINCIPAL");
        await _ordenCompraRepository.DidNotReceive().AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsumosDeDosProveedores_CreaUnaOrdenPorProveedor()
    {
        var proveedorA = Guid.NewGuid();
        var proveedorB = Guid.NewGuid();
        var insumoA = CrearInsumo(proveedorA, stockMinimo: 10, stockActual: 3);
        var insumoB = CrearInsumo(proveedorB, stockMinimo: 10, stockActual: 3);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumoA, insumoB });
        _ordenCompraRepository.GetNextNumeroAsync(Arg.Any<CancellationToken>()).Returns(1, 2);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var ordenes = new List<OrdenCompra>();
        _ordenCompraRepository.When(r => r.AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>()))
            .Do(ci => ordenes.Add(ci.Arg<OrdenCompra>()));

        var result = await CreateHandler().HandleAsync(new GenerarOCDesdeAlertasCommand(new[] { insumoA.Id, insumoB.Id }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ordenes.Count.ShouldBe(2);
        ordenes.Count.ShouldBe(2);
        ordenes.Select(o => o.ProveedorId).ShouldBe(new[] { proveedorA, proveedorB }, ignoreOrder: true);
        ordenes.ShouldAllBe(o => o.Items.Count == 1);
        ordenes.ShouldAllBe(o => o.Items.Single().CantidadPedida == 7m);
    }

    [Fact]
    public async Task HandleAsync_InsumosDeDosProveedores_AsignaNumerosSecuencialesSinDuplicados()
    {
        var proveedorA = Guid.NewGuid();
        var proveedorB = Guid.NewGuid();
        var insumoA = CrearInsumo(proveedorA, stockMinimo: 10, stockActual: 3);
        var insumoB = CrearInsumo(proveedorB, stockMinimo: 10, stockActual: 3);
        _insumoRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(new[] { insumoA, insumoB });
        // Regression: GetNextNumeroAsync is called ONCE per request; pending adds are
        // invisible to Max() before SaveChanges, so sequential numbers must be derived locally.
        _ordenCompraRepository.GetNextNumeroAsync(Arg.Any<CancellationToken>()).Returns(5);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var ordenes = new List<OrdenCompra>();
        _ordenCompraRepository.When(r => r.AddAsync(Arg.Any<OrdenCompra>(), Arg.Any<CancellationToken>()))
            .Do(ci => ordenes.Add(ci.Arg<OrdenCompra>()));

        var result = await CreateHandler().HandleAsync(new GenerarOCDesdeAlertasCommand(new[] { insumoA.Id, insumoB.Id }));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ordenes.Count.ShouldBe(2);
        await _ordenCompraRepository.Received(1).GetNextNumeroAsync(Arg.Any<CancellationToken>());
        ordenes.Select(o => o.Numero).ShouldBe(new[] { 5, 6 }, ignoreOrder: true);
        ordenes.Select(o => o.Numero).ShouldBeUnique();
    }
}