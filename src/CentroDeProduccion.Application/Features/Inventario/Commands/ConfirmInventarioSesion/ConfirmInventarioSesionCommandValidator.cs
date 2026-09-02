using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;

public class ConfirmInventarioSesionCommandValidator : AbstractValidator<ConfirmInventarioSesionCommand>
{
    public ConfirmInventarioSesionCommandValidator()
    {
        RuleFor(x => x.InventarioSesionId)
            .NotEmpty().WithMessage("La sesión de inventario es requerida");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("La versión de concurrencia es requerida");
    }
}
