using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;

public class RegisterNotaCreditoCommandValidator : AbstractValidator<RegisterNotaCreditoCommand>
{
    public RegisterNotaCreditoCommandValidator()
    {
        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("El proveedor es requerido");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero");
        RuleFor(x => x.Referencia)
            .MaximumLength(500).When(x => x.Referencia is not null)
            .WithMessage("La referencia no puede superar los 500 caracteres");
    }
}