using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;

public class RegistrarConteoCommandValidator : AbstractValidator<RegistrarConteoCommand>
{
    public RegistrarConteoCommandValidator()
    {
        RuleFor(x => x.InventarioSesionId)
            .NotEmpty().WithMessage("La sesión de inventario es requerida");

        RuleFor(x => x.ConteoId)
            .NotEmpty().WithMessage("El conteo es requerido");

        RuleFor(x => x.CantidadContada)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad contada no puede ser negativa");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).WithMessage("Las observaciones no pueden superar los 500 caracteres");
    }
}
