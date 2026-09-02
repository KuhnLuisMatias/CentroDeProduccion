using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Abstractions.Security;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using FluentValidation;
using NSubstitute;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Handlers;

/// <summary>
/// Verifies the atomic stock write-back, unit conversion wiring (design D6) and the
/// insufficient-stock guard in <see cref="RegisterMovementCommandHandler"/>.
/// </summary>
public class RegisterMovementCommandHandlerTests
{
    private readonly IInsumoRepository _insumoRepository = Substitute.For<IInsumoRepository>();
    private readonly IProductoTerminadoRepository _productoTerminadoRepository = Substitute.For<IProductoTerminadoRepository>();
    private readonly IMovimientoStockRepository _movimientoRepository = Substitute.For<IMovimientoStockRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IValidator<RegisterMovementCommand> _validator = new RegisterMovementCommandValidator();

    private RegisterMovementCommandHandler CreateHandler() => new(
        _insumoRepository, _productoTerminadoRepository, _movimientoRepository, _unitOfWork, _currentUser, _validator);

    private static (Insumo insumo, Guid unidadCompraId, Guid unidadConsumoId) CreateInsumo(
        decimal stockActual, decimal factorConversion = 1)
    {
        var unidadCompraId = Guid.NewGuid();
        var unidadConsumoId = Guid.NewGuid();
        var insumo = new Insumo
        {
            Id = Guid.NewGuid(),
            Nombre = "Carne picada",
            CodigoSku = "CARNE-001",
            CategoriaId = Guid.NewGuid(),
            UnidadCompraId = unidadCompraId,
            UnidadConsumoId = unidadConsumoId,
            FactorConversion = factorConversion,
            StockActual = stockActual,
            StockMinimo = 10,
            Activo = true
        };
        return (insumo, unidadCompraId, unidadConsumoId);
    }

    private static RegisterMovementCommand CompraCommand(Guid insumoId, Guid unidadCompraId, decimal cantidad = 50m) => new(
        insumoId, null, TipoMovimientoStock.Compra, cantidad, unidadCompraId, 18000m, "Compra", "OC-001");

    private static RegisterMovementCommand AjusteNegativoCommand(Guid insumoId, Guid unidadCompraId, decimal cantidad) => new(
        insumoId, null, TipoMovimientoStock.AjusteNegativo, cantidad, unidadCompraId, null, "Merma", null);

    [Fact]
    public async Task HandleAsync_AjusteNegativoWithInsufficientStock_ReturnsValidationError()
    {
        var (insumo, unidadCompraId, _) = CreateInsumo(stockActual: 5m);
        _insumoRepository.GetByIdAsync(insumo.Id).Returns(insumo);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(AjusteNegativoCommand(insumo.Id, unidadCompraId, 10m));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUFFICIENT_STOCK");
        await _movimientoRepository.DidNotReceive().AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Compra_WritesBackStockAtomically()
    {
        var (insumo, unidadCompraId, _) = CreateInsumo(stockActual: 0m);
        _insumoRepository.GetByIdAsync(insumo.Id).Returns(insumo);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(CompraCommand(insumo.Id, unidadCompraId));

        result.IsSuccess.ShouldBeTrue();
        insumo.StockActual.ShouldBe(50m);
        insumo.PrecioUltimaCompra.ShouldBe(18000m);
        await _movimientoRepository.Received(1).AddAsync(Arg.Any<MovimientoStock>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompraInPurchaseUnit_ConvertsToConsumptionUnit()
    {
        // 1 purchase unit (kg) = 1000 consumption units (g); buying 2 kg => +2000 g
        var (insumo, unidadCompraId, _) = CreateInsumo(stockActual: 0m, factorConversion: 1000m);
        _insumoRepository.GetByIdAsync(insumo.Id).Returns(insumo);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(CompraCommand(insumo.Id, unidadCompraId, cantidad: 2m));

        result.IsSuccess.ShouldBeTrue();
        insumo.StockActual.ShouldBe(2000m);
        result.Value.CantidadMovimiento.ShouldBe(2000m);
    }

    [Fact]
    public async Task HandleAsync_Compra_UpdatesPrecioUltimaCompra()
    {
        var (insumo, unidadCompraId, _) = CreateInsumo(stockActual: 50m);
        insumo.PrecioUltimaCompra = 10000m;
        _insumoRepository.GetByIdAsync(insumo.Id).Returns(insumo);
        _currentUser.UsuarioId.Returns(Guid.NewGuid());

        var result = await CreateHandler().HandleAsync(CompraCommand(insumo.Id, unidadCompraId, cantidad: 50m));

        result.IsSuccess.ShouldBeTrue();
        insumo.PrecioUltimaCompra.ShouldBe(18000m);
    }

    [Fact]
    public async Task HandleAsync_InsumoNotFound_ReturnsNotFound()
    {
        _insumoRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Insumo?)null);

        var result = await CreateHandler().HandleAsync(CompraCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INSUMO_NOT_FOUND");
    }
}
