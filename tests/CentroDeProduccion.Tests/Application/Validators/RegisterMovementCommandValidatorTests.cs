using CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;
using CentroDeProduccion.Domain.Enums;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Validators;

/// <summary>
/// Verifies the dual-target movement rules: insumo types require an insumo, product types
/// require a finished product, and exactly one target must be set.
/// </summary>
public class RegisterMovementCommandValidatorTests
{
    private readonly RegisterMovementCommandValidator _validator = new();

    private static RegisterMovementCommand InsumoCommand(TipoMovimientoStock tipo, decimal? precio = null) => new(
        Guid.NewGuid(), null, tipo, 10m, Guid.NewGuid(), precio, "Motivo", null);

    private static RegisterMovementCommand ProductoCommand(TipoMovimientoStock tipo) => new(
        null, Guid.NewGuid(), tipo, 10m, Guid.NewGuid(), null, "Motivo", null);

    [Theory]
    [InlineData(TipoMovimientoStock.Compra)]
    [InlineData(TipoMovimientoStock.AjustePositivo)]
    [InlineData(TipoMovimientoStock.AjusteNegativo)]
    public void Validate_InsumoTypes_Pass(TipoMovimientoStock tipo)
    {
        var precio = tipo == TipoMovimientoStock.Compra ? 18000m : (decimal?)null;
        _validator.Validate(InsumoCommand(tipo, precio)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(TipoMovimientoStock.Produccion)]
    [InlineData(TipoMovimientoStock.VentaBar)]
    [InlineData(TipoMovimientoStock.DevolucionBar)]
    [InlineData(TipoMovimientoStock.BajaPorVencimiento)]
    public void Validate_ProductoTypes_Pass(TipoMovimientoStock tipo)
    {
        _validator.Validate(ProductoCommand(tipo)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CompraWithoutPrice_Fails()
    {
        var result = _validator.Validate(InsumoCommand(TipoMovimientoStock.Compra, null));
        result.Errors.ShouldContain(e => e.PropertyName == "PrecioUnitario");
    }

    [Fact]
    public void Validate_InsumoTypeWithProducto_Fails()
    {
        var command = InsumoCommand(TipoMovimientoStock.Compra, 18000m) with
        {
            InsumoId = null,
            ProductoTerminadoId = Guid.NewGuid()
        };
        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_ZeroQuantity_Fails()
    {
        var command = InsumoCommand(TipoMovimientoStock.AjustePositivo) with { Cantidad = 0 };
        _validator.Validate(command).IsValid.ShouldBeFalse();
    }
}
