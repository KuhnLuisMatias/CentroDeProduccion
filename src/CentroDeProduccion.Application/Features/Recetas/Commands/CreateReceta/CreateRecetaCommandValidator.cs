using FluentValidation;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandValidator : AbstractValidator<CreateRecetaCommand>
{
    public CreateRecetaCommandValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200);

        RuleFor(x => x.CodigoSku)
            .NotEmpty().WithMessage("El SKU es requerido")
            .MaximumLength(50);

        RuleFor(x => x.CategoriaId)
            .NotEmpty().WithMessage("La categoría es requerida");

        RuleFor(x => x.UnidadMedidaId)
            .NotEmpty().WithMessage("La unidad de medida resultante es requerida");

        RuleFor(x => x.Insumos)
            .NotEmpty().WithMessage("La receta debe tener al menos un insumo");

        RuleForEach(x => x.Insumos).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.CantidadNecesaria)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");
            detalle.RuleFor(d => d)
                .Must(d => (d.InsumoId.HasValue) != (d.RecetaOrigenId.HasValue))
                .WithMessage("Debe indicar exactamente un insumo O una sub-receta, no ambos ni ninguno");
        });
    }
}
