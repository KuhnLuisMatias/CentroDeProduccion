using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Insumos.Commands.ReactivateInsumo;
using CentroDeProduccion.Domain.Entities;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

public class ReactivateInsumoCommandHandlerTests
{
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ReactivateInsumoCommandHandler CreateHandler() => new(_insumoRepository, _unitOfWork);

    private static Insumo Insumo(bool activo) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Harina 000",
        CodigoSku = "INS-001",
        Activo = activo,
    };

    [Fact]
    public async Task HandleAsync_InsumoInexistente_RetornaNotFound()
    {
        _insumoRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Insumo?)null);

        var result = await CreateHandler().HandleAsync(new ReactivateInsumoCommand(Guid.NewGuid()));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("INSUMO_NOT_FOUND");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsumoYaActivo_RetornaValidation()
    {
        var insumo = Insumo(activo: true);
        _insumoRepository.GetByIdAsync(insumo.Id, Arg.Any<CancellationToken>()).Returns(insumo);

        var result = await CreateHandler().HandleAsync(new ReactivateInsumoCommand(insumo.Id));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("INSUMO_YA_ACTIVO");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsumoInactivo_ActivaYPersiste()
    {
        var insumo = Insumo(activo: false);
        _insumoRepository.GetByIdAsync(insumo.Id, Arg.Any<CancellationToken>()).Returns(insumo);

        var result = await CreateHandler().HandleAsync(new ReactivateInsumoCommand(insumo.Id));

        result.IsSuccess.ShouldBeTrue();
        insumo.Activo.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
