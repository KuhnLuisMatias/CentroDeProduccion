using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Empleados.Commands.DeleteEmpleado;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the soft-delete behavior: the employee row is never removed, only Activo=false.
/// </summary>
public class DeleteEmpleadoCommandHandlerTests
{
    private readonly IEmpleadoRepository _empleadoRepository = Substitute.For<IEmpleadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteEmpleadoCommandHandler CreateHandler() => new(_empleadoRepository, _unitOfWork);

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

    [Fact]
    public async Task HandleAsync_EmpleadoExistente_SoftDeletes()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);

        var result = await CreateHandler().HandleAsync(new DeleteEmpleadoCommand(empleado.Id, empleado.RowVersion));

        result.IsSuccess.ShouldBeTrue();
        empleado.Activo.ShouldBeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmpleadoNoExiste_ReturnsNotFound()
    {
        _empleadoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Empleado?)null);

        var result = await CreateHandler().HandleAsync(new DeleteEmpleadoCommand(Guid.NewGuid(), new byte[] { 1, 2, 3 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("EMPLEADO_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_RowVersionDesactualizada_ReturnsConcurrency()
    {
        var empleado = CrearEmpleado();
        _empleadoRepository.GetByIdAsync(empleado.Id).Returns(empleado);

        var result = await CreateHandler().HandleAsync(new DeleteEmpleadoCommand(empleado.Id, new byte[] { 9, 9, 9 }));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("CONCURRENCY_CONFLICT");
        empleado.Activo.ShouldBeTrue();
    }
}