using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaCredito;

public class RegisterNotaCreditoCommandValidator : AbstractValidator<RegisterNotaCreditoCommand>
{
    public RegisterNotaCreditoCommandValidator()
    {
        RuleFor(x => x.BarId)
            .NotEmpty().WithMessage("El bar es requerido");
        RuleFor(x => x.Monto)
            .NotEqual(0).WithMessage("El monto no puede ser cero");
        RuleFor(x => x.Referencia)
            .MaximumLength(500).When(x => x.Referencia is not null)
            .WithMessage("La referencia no puede superar los 500 caracteres");
    }
}