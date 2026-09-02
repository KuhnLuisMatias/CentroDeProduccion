using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Recetas.Commands.UpdateReceta;

public class UpdateRecetaCommandValidator : AbstractValidator<UpdateRecetaCommand>
{
    public UpdateRecetaCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CodigoSku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.UnidadMedidaId).NotEmpty().WithMessage("La unidad de medida resultante es requerida");
        RuleFor(x => x.Estado).IsInEnum();
        RuleFor(x => x.Insumos).NotEmpty();

        RuleForEach(x => x.Insumos).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.CantidadNecesaria).GreaterThan(0);
            detalle.RuleFor(d => d.UnidadMedidaId).NotEmpty();
            detalle.RuleFor(d => d)
                .Must(d => (d.InsumoId.HasValue) != (d.RecetaOrigenId.HasValue))
                .WithMessage("Debe indicar exactamente un insumo O una sub-receta");
        });
    }
}
