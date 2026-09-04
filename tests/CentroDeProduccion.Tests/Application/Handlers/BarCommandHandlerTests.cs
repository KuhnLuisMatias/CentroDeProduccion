using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.DeleteBar;
using CentroDeProduccion.Application.Features.Bares.Commands.ReactivateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.UpdateBar;
using CentroDeProduccion.Application.Features.Bares.Queries;
using CentroDeProduccion.Application.Features.Bares.Queries.GetBarList;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies bar creation: Activo by default, the MargenReventa snapshot, the unique-nombre
/// guard and the validator's required-field / non-negative-margin rules.
/// </summary>
public class CreateBarCommandHandlerTests
{
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<CreateBarCommand> _validator = new CreateBarCommandValidator();

    private CreateBarCommandHandler CreateHandler() => new(_barRepository, _unitOfWork, _validator);

    private static CreateBarCommand Command(string nombre = "Bar Centro", string direccion = "Av. Siempre Viva 123", decimal margen = 0m) => new(
        nombre, direccion, null, null, null, margen);

    [Fact]
    public async Task HandleAsync_BarValido_CreaActivoConMargenDefaultCero()
    {
        _barRepository.ExistsWithNombreAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Bar? creado = null;
        _barRepository.When(r => r.AddAsync(Arg.Any<Bar>(), Arg.Any<CancellationToken>()))
            .Do(ci => creado = ci.Arg<Bar>());

        var result = await CreateHandler().HandleAsync(Command());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Estado.ShouldBe(EstadoBar.Activo);
        result.Value.Nombre.ShouldBe("Bar Centro");
        creado.ShouldNotBeNull();
        creado!.Nombre.ShouldBe("Bar Centro");
        creado.Estado.ShouldBe(EstadoBar.Activo);
        creado.MargenReventaPorcentaje.ShouldBe(0m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NombreDuplicado_ReturnsConflict()
    {
        _barRepository.ExistsWithNombreAsync("Bar Centro", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("BAR_NOMBRE_DUPLICADO");
        result.Error.Message.ShouldContain("Ya existe un bar con este nombre");
        await _barRepository.DidNotReceive().AddAsync(Arg.Any<Bar>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DireccionFaltante_ReturnsValidationError()
    {
        var result = await CreateHandler().HandleAsync(Command(direccion: string.Empty));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("Direccion");
        await _barRepository.DidNotReceive().AddAsync(Arg.Any<Bar>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MargenNegativo_ReturnsValidationError()
    {
        var result = await CreateHandler().HandleAsync(Command(margen: -5m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("MargenReventaPorcentaje");
        await _barRepository.DidNotReceive().AddAsync(Arg.Any<Bar>(), Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies bar edits: field write-back, the optimistic-concurrency guard and the
/// unique-nombre check that excludes the bar being edited.
/// </summary>
public class UpdateBarCommandHandlerTests
{
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<UpdateBarCommand> _validator = new UpdateBarCommandValidator();

    private UpdateBarCommandHandler CreateHandler() => new(_barRepository, _unitOfWork, _validator);

    private static Bar CrearBar(byte[] rowVersion) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Bar Centro",
        Direccion = "Av. Siempre Viva 123",
        MargenReventaPorcentaje = 10m,
        Estado = EstadoBar.Activo,
        RowVersion = rowVersion
    };

    private static UpdateBarCommand Command(Guid barId, byte[] rowVersion) => new(
        barId, "Bar Sur", "Calle Falsa 456", "Juan", "555-1234", "10-18", 25m, rowVersion, EstadoBar.Inactivo);

    [Fact]
    public async Task HandleAsync_BarValido_ActualizaCampos()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var bar = CrearBar(rowVersion);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _barRepository.ExistsWithNombreAsync("Bar Sur", bar.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        bar.Nombre.ShouldBe("Bar Sur");
        bar.Direccion.ShouldBe("Calle Falsa 456");
        bar.Encargado.ShouldBe("Juan");
        bar.Telefono.ShouldBe("555-1234");
        bar.HorarioRecepcion.ShouldBe("10-18");
        bar.MargenReventaPorcentaje.ShouldBe(25m);
        bar.Estado.ShouldBe(EstadoBar.Inactivo); // seeded Activo — proves estado write-back
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RowVersionDistinta_ReturnsConcurrency()
    {
        var bar = CrearBar(new byte[] { 1, 2, 3 });
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, new byte[] { 9, 9, 9 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Concurrency);
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
        await _barRepository.DidNotReceive()
            .ExistsWithNombreAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NombreDuplicadoExcluyendoSelf_ReturnsConflict()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var bar = CrearBar(rowVersion);
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);
        _barRepository.ExistsWithNombreAsync("Bar Sur", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().HandleAsync(Command(bar.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("BAR_NOMBRE_DUPLICADO");
        await _barRepository.Received(1)
            .ExistsWithNombreAsync("Bar Sur", Arg.Is<Guid?>(g => g == bar.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies the soft delete: a matching RowVersion flips Estado to Inactivo instead of
/// removing the row, and unknown bars return BAR_NOT_FOUND.
/// </summary>
public class DeleteBarCommandHandlerTests
{
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteBarCommandHandler CreateHandler() => new(_barRepository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_BarExistente_DesactivaSinEliminar()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var bar = new Bar { Id = Guid.NewGuid(), Nombre = "Bar Centro", Estado = EstadoBar.Activo, RowVersion = rowVersion };
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(new DeleteBarCommand(bar.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        bar.Estado.ShouldBe(EstadoBar.Inactivo);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BarInexistente_ReturnsNotFound()
    {
        var result = await CreateHandler().HandleAsync(new DeleteBarCommand(Guid.NewGuid(), new byte[] { 1, 2, 3 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("BAR_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies reactivation: Inactivo bares flip back to Activo, while already-active bares are
/// rejected with BAR_YA_ACTIVO.
/// </summary>
public class ReactivateBarCommandHandlerTests
{
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ReactivateBarCommandHandler CreateHandler() => new(_barRepository, _unitOfWork);

    [Fact]
    public async Task HandleAsync_BarInactivo_Reactiva()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var bar = new Bar { Id = Guid.NewGuid(), Nombre = "Bar Centro", Estado = EstadoBar.Inactivo, RowVersion = rowVersion };
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(new ReactivateBarCommand(bar.Id, rowVersion));

        result.IsSuccess.ShouldBeTrue();
        bar.Estado.ShouldBe(EstadoBar.Activo);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_BarYaActivo_ReturnsValidationError()
    {
        var rowVersion = new byte[] { 1, 2, 3 };
        var bar = new Bar { Id = Guid.NewGuid(), Nombre = "Bar Centro", Estado = EstadoBar.Activo, RowVersion = rowVersion };
        _barRepository.GetByIdAsync(bar.Id).Returns(bar);

        var result = await CreateHandler().HandleAsync(new ReactivateBarCommand(bar.Id, rowVersion));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("BAR_YA_ACTIVO");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>
/// Verifies the list query forwards the estado/searchTerm filters to the repository and maps
/// the results to list items.
/// </summary>
public class GetBarListQueryHandlerTests
{
    private readonly IBarRepository _barRepository = Substitute.For<IBarRepository>();

    private static Bar CrearBar(string nombre, EstadoBar estado) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Direccion = "Av. Siempre Viva 123",
        MargenReventaPorcentaje = 10m,
        Estado = estado
    };

    [Fact]
    public async Task HandleAsync_ConFiltros_DelegaYDevuelveLista()
    {
        var activo = CrearBar("Bar Centro", EstadoBar.Activo);
        var inactivo = CrearBar("Bar Norte", EstadoBar.Inactivo);
        _barRepository.GetByFiltersAsync(Arg.Any<EstadoBar?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { activo, inactivo });

        var handler = new GetBarListQueryHandler(_barRepository);
        var result = await handler.HandleAsync(new GetBarListQuery(EstadoBar.Activo, "Centro"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(i => i.Nombre == "Bar Centro" && i.Estado == EstadoBar.Activo);
        result.Value.ShouldContain(i => i.Nombre == "Bar Norte" && i.MargenReventaPorcentaje == 10m);
        await _barRepository.Received(1)
            .GetByFiltersAsync(EstadoBar.Activo, "Centro", Arg.Any<CancellationToken>());
    }
}