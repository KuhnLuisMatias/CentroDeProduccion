using FluentValidation;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;

public class GenerarOCDesdeAlertasCommandValidator : AbstractValidator<GenerarOCDesdeAlertasCommand>
{
    public GenerarOCDesdeAlertasCommandValidator()
    {
        RuleFor(x => x.InsumoIds)
            .NotEmpty().WithMessage("Debe seleccionar al menos un insumo");
    }
}