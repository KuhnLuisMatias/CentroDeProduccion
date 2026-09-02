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
            linea.RuleFor(l => l.InsumoId)
                .NotEmpty().WithMessage("El insumo es requerido");
            linea.RuleFor(l => l.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");
        });
    }
}
