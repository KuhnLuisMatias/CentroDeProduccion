using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.UnidadesMedida.Commands.UpdateUnidadMedida;

public class UpdateUnidadMedidaCommandValidator : AbstractValidator<UpdateUnidadMedidaCommand>
{
    public UpdateUnidadMedidaCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID es requerido");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres");

        RuleFor(x => x.Simbolo)
            .NotEmpty().WithMessage("El símbolo es requerido")
            .MaximumLength(10).WithMessage("El símbolo no puede exceder 10 caracteres");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo no es válido");
    }
}
