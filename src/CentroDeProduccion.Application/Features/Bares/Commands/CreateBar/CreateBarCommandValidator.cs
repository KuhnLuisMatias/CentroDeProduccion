using FluentValidation;

namespace CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;

public class CreateBarCommandValidator : AbstractValidator<CreateBarCommand>
{
    public CreateBarCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Direccion).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Encargado).MaximumLength(100);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.HorarioRecepcion).MaximumLength(100);
        RuleFor(x => x.MargenReventaPorcentaje).GreaterThanOrEqualTo(0);
    }
}