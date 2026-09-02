using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies employee creation: default active state and DNI-uniqueness guard.
/// </summary>
public class CreateEmpleadoCommandHandlerTests
{
    private readonly IEmpleadoRepository _empleadoRepository = Substitute.For<IEmpleadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IValidator<CreateEmpleadoCommand> _validator = new CreateEmpleadoCommandValidator();

    private CreateEmpleadoCommandHandler CreateHandler() => new(_empleadoRepository, _unitOfWork, _validator);

    private static CreateEmpleadoCommand Command() => new(
        "Juan", "Perez", "12345678", CargoEmpleado.Cocinero, 100m, CategoriaEmpleado.Produccion);

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesActiveEmpleado()
    {
        Empleado? creado = null;
        _empleadoRepository.When(r => r.AddAsync(Arg.Any<Empleado>(), Arg.Any<CancellationToken>()))
            .Do(ci => creado = ci.Arg<Empleado>());
        _empleadoRepository.ExistsWithDniAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsSuccess.ShouldBeTrue();
        result.Value.Dni.ShouldBe("12345678");
        creado.ShouldNotBeNull();
        creado!.Activo.ShouldBeTrue();
        creado.TarifaPorHora.ShouldBe(100m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DniDuplicado_ReturnsConflict()
    {
        _empleadoRepository.ExistsWithDniAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("DNI_ALREADY_EXISTS");
        await _empleadoRepository.DidNotReceive().AddAsync(Arg.Any<Empleado>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}