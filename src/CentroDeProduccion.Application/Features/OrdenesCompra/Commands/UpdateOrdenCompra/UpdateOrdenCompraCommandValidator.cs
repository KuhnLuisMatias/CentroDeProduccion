using FluentValidation;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;

public class UpdateOrdenCompraCommandValidator : AbstractValidator<UpdateOrdenCompraCommand>
{
    public UpdateOrdenCompraCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("El proveedor es requerido");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("El RowVersion es requerido");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("La orden debe tener al menos un item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.InsumoId)
                .NotEmpty().WithMessage("El insumo es requerido");
            item.RuleFor(i => i.CantidadPedida)
                .GreaterThan(0).WithMessage("La cantidad pedida debe ser mayor a cero");
            item.RuleFor(i => i.PrecioUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");
        });
    }
}