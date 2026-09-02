using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandValidator : AbstractValidator<CreateEmpleadoCommand>
{
    public CreateEmpleadoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dni).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Cargo).IsInEnum();
        RuleFor(x => x.Categoria).IsInEnum();
        RuleFor(x => x.TarifaPorHora).GreaterThan(0).WithMessage("La tarifa por hora debe ser mayor a cero");
    }
}
