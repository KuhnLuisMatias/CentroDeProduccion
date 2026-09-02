using FluentValidation;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;

public class CreateProductoTerminadoCommandValidator : AbstractValidator<CreateProductoTerminadoCommand>
{
    public CreateProductoTerminadoCommandValidator()
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
            .NotEmpty().WithMessage("La unidad de medida es requerida");
    }
}
