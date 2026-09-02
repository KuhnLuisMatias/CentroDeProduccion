using FluentValidation;

namespace CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;

public class CreatePagoProveedorCommandValidator : AbstractValidator<CreatePagoProveedorCommand>
{
    public CreatePagoProveedorCommandValidator()
    {
        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("El proveedor es requerido");

        RuleFor(x => x.FechaPago)
            .NotEmpty().WithMessage("La fecha de pago es requerida");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");

        RuleFor(x => x.Insumos)
            .NotEmpty().WithMessage("Debe indicar al menos un insumo");

        RuleForEach(x => x.Insumos).ChildRules(insumo =>
        {
            insumo.RuleFor(i => i.InsumoId)
                .NotEmpty().WithMessage("El insumo es requerido");
            insumo.RuleFor(i => i.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");
            insumo.RuleFor(i => i.PrecioUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");
        });
    }
}
