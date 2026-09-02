using FluentValidation;

namespace CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;

public class CreateProduccionCommandValidator : AbstractValidator<CreateProduccionCommand>
{
    public CreateProduccionCommandValidator()
    {
        RuleFor(x => x.RecetaId)
            .NotEmpty().WithMessage("La receta es requerida");
    }
}
