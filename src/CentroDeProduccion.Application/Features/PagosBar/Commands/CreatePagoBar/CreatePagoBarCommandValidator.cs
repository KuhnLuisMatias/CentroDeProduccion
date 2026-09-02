using FluentValidation;

namespace CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;

public class CreatePagoBarCommandValidator : AbstractValidator<CreatePagoBarCommand>
{
    public CreatePagoBarCommandValidator()
    {
        RuleFor(x => x.BarId)
            .NotEmpty().WithMessage("El bar es requerido");

        RuleFor(x => x.MontoTotal)
            .GreaterThan(0).WithMessage("El monto total debe ser mayor a cero");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");

        RuleFor(x => x.Metodos)
            .NotEmpty().WithMessage("Debe indicar al menos un método de pago");

        RuleForEach(x => x.Metodos).ChildRules(metodo =>
        {
            metodo.RuleFor(m => m.Tipo)
                .IsInEnum().WithMessage("El método de pago no es válido");
            metodo.RuleFor(m => m.Monto)
                .GreaterThan(0).WithMessage("El monto del método de pago debe ser mayor a cero");
            metodo.RuleFor(m => m.Referencia)
                .MaximumLength(200).When(m => !string.IsNullOrEmpty(m.Referencia))
                .WithMessage("La referencia no puede superar los 200 caracteres");
        });

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Debe indicar al menos una asignación");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.RemitoId)
                .NotEmpty().WithMessage("El remito es requerido");
            item.RuleFor(i => i.MontoAplicado)
                .GreaterThan(0).WithMessage("El monto aplicado debe ser mayor a cero");
        });
    }
}