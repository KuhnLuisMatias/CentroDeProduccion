using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies employee update: field mutation, DNI immutability (spec §8.1), DNI uniqueness and
/// optimistic-concurrency guard.
/// </summary>
public class UpdateEmpleadoCommandHandlerTests
{
    private readonly IEmpleadoRepository _empleadoRepository = Substitute.For<IEmpleadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<UpdateEmpleadoCommand> _validator = new UpdateEmpleadoCommandValidator();

    private UpdateEmpleadoCommandHandler CreateHandler() => new(_empleadoRepository, _unitOfWork, _validator);

    private static Empleado CrearEmpleado() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Juan",
        Apellido = "Perez",
        Dni = "12345678",
        Cargo = CargoEmpleado.Cocinero,
        TarifaPorHora = 100m,
        Categoria = CategoriaEmpleado.Produccion,
        Activo = true,
        RowVersion = new byte[] { 1, 2, 3 }
    };

    private static UpdateEmpleadoCommand Command(Empleado e, string dni = "12345678", bool activo = true) => new(
        e.Id, e.Nombre, e.Apellido, dni, e.Cargo, e.TarifaPorHora, e.Categoria, activo, e.RowVersion);

    [Fact]
    public async Task HandleAsync_ValidCommand_UpdatesFields()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);
        _empleadoRepository.ExistsWithDniAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(Command(empleado, activo: false));

        result.IsSuccess.ShouldBeTrue();
        empleado.Activo.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DniCambiado_ReturnsValidationError()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);

        var result = await CreateHandler().HandleAsync(Command(empleado, dni: "99999999"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("DNI_INMUTABLE");
        empleado.Dni.ShouldBe("12345678");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DniDuplicado_ReturnsConflict()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);
        _empleadoRepository.ExistsWithDniAsync(empleado.Dni, empleado.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().HandleAsync(Command(empleado));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("DNI_ALREADY_EXISTS");
    }

    [Fact]
    public async Task HandleAsync_EmpleadoNoExiste_ReturnsNotFound()
    {
        _empleadoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Empleado?)null);

        var result = await CreateHandler().HandleAsync(Command(CrearEmpleado()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("EMPLEADO_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_RowVersionDesactualizada_ReturnsConcurrency()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);
        _empleadoRepository.ExistsWithDniAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(Command(empleado) with { RowVersion = new byte[] { 9, 9, 9 } });

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
    }
}