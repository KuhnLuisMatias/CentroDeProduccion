using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;

public class CreateInventarioSesionCommandValidator : AbstractValidator<CreateInventarioSesionCommand>
{
    public CreateInventarioSesionCommandValidator()
    {
        RuleFor(x => x.TipoInventario)
            .IsInEnum().WithMessage("Tipo de inventario no válido");

        RuleFor(x => x.Notas)
            .MaximumLength(1000).WithMessage("Las notas no pueden superar los 1000 caracteres");
    }
}
