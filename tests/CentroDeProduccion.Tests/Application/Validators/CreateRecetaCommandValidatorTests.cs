using CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
using Shouldly;

namespace CentroDeProduccion.Tests.Application.Validators;

/// <summary>
/// Verifies the BOM exclusivity rule: each recipe line must be exactly one of a direct insumo
/// OR a sub-recipe (never both, never neither).
/// </summary>
public class CreateRecetaCommandValidatorTests
{
    private readonly CreateRecetaCommandValidator _validator = new();

    private static CreateRecetaCommand BaseCommand(params RecetaInsumoDto[] insumos) => new(
        "Milanesa", "MILA-001", Guid.NewGuid(), Guid.NewGuid(), null, insumos);

    [Fact]
    public void Validate_InsumoDirecto_Passes()
    {
        var command = BaseCommand(new RecetaInsumoDto(Guid.NewGuid(), null, 5m, Guid.NewGuid(), null));
        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SubReceta_Passes()
    {
        var command = BaseCommand(new RecetaInsumoDto(null, Guid.NewGuid(), 2m, Guid.NewGuid(), null));
        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, true)]   // both insumo and sub-recipe
    [InlineData(false, false)] // neither
    public void Validate_NoExclusivo_Fails(bool conInsumo, bool conSubReceta)
    {
        var command = BaseCommand(new RecetaInsumoDto(
            conInsumo ? Guid.NewGuid() : null,
            conSubReceta ? Guid.NewGuid() : null,
            5m, Guid.NewGuid(), null));

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_SinInsumos_Fails()
    {
        _validator.Validate(BaseCommand()).IsValid.ShouldBeFalse();
    }
}
