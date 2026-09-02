using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandValidator : AbstractValidator<UpdateEmpleadoCommand>
{
    public UpdateEmpleadoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Apellido).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dni).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Cargo).IsInEnum();
        RuleFor(x => x.Categoria).IsInEnum();
        RuleFor(x => x.TarifaPorHora).GreaterThan(0);
        RuleFor(x => x.Activo).NotNull();
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
