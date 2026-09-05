using FluentValidation;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;

public class EditarInsumosProduccionCommandValidator : AbstractValidator<EditarInsumosProduccionCommand>
{
    public EditarInsumosProduccionCommandValidator()
    {
        RuleFor(x => x.ProduccionId)
            .NotEmpty().WithMessage("La producción es requerida");

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("Debe declarar al menos un insumo consumido");

        RuleForEach(x => x.Lineas).ChildRules(linea =>
        {
            linea.RuleFor(l => l.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");

            // Exactly one origin, mirroring RecetaInsumo/ProduccionInsumo.
            linea.RuleFor(l => l)
                .Must(l => l.InsumoId.HasValue != l.RecetaOrigenId.HasValue)
                .WithMessage("Cada línea debe referenciar un insumo o una subreceta, no ambos ni ninguno");
        });
    }
}
