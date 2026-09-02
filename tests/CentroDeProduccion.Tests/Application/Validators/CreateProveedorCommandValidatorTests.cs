using CentroDeProduccion.Application.Features.Proveedores.Commands.CreateProveedor;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Validators;

/// <summary>
/// Verifies the CUIT format rule (design decision: regex only for Phase 1) plus the
/// TipoFactura domain constraint. CUIT must match XX-XXXXXXXX-X.
/// </summary>
public class CreateProveedorCommandValidatorTests
{
    private readonly CreateProveedorCommandValidator _validator = new();

    private static CreateProveedorCommand BaseCommand(string cuit, string tipoFactura = "A") => new(
        "Proveedor Test", cuit, "Av Siempre Viva 123",
        null, null, null, null, null, "Carnes,Lacteos",
        tipoFactura, null);

    [Fact]
    public void Validate_ValidCuit_Passes()
    {
        var result = _validator.Validate(BaseCommand("30-12345678-9"));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("30123456789")]       // sin guiones
    [InlineData("30-12345678")]       // faltan digitos
    [InlineData("30-12345678-99")]    // digito verificador largo
    [InlineData("abc-defghij-k")]     // letras
    [InlineData("")]                  // vacío
    public void Validate_InvalidCuit_Fails(string cuit)
    {
        var result = _validator.Validate(BaseCommand(cuit));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Cuit");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void Validate_ValidTipoFactura_Passes(string tipoFactura)
    {
        var result = _validator.Validate(BaseCommand("30-12345678-9", tipoFactura));
        result.Errors.ShouldNotContain(e => e.PropertyName == "TipoFactura");
    }

    [Theory]
    [InlineData("D")]
    [InlineData("X")]
    [InlineData("")]
    public void Validate_InvalidTipoFactura_Fails(string tipoFactura)
    {
        var result = _validator.Validate(BaseCommand("30-12345678-9", tipoFactura));
        result.Errors.ShouldContain(e => e.PropertyName == "TipoFactura");
    }
}
