using FluentValidation;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.UpdateProductoTerminado;

public class UpdateProductoTerminadoCommandValidator : AbstractValidator<UpdateProductoTerminadoCommand>
{
    public UpdateProductoTerminadoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CodigoSku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.UnidadMedidaId).NotEmpty();
        RuleFor(x => x.StockMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
