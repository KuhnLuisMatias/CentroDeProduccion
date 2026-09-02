using FluentValidation;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;

public class ConfirmRemitoCommandValidator : AbstractValidator<ConfirmRemitoCommand>
{
    public ConfirmRemitoCommandValidator()
    {
        RuleFor(x => x.RemitoId)
            .NotEmpty().WithMessage("El remito es requerido");
    }
}