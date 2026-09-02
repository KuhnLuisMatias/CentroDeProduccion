using FluentValidation;

namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

public class CreateDevolucionCommandValidator : AbstractValidator<CreateDevolucionCommand>
{
    public CreateDevolucionCommandValidator()
    {
        RuleFor(x => x.RemitoId)
            .NotEmpty().WithMessage("El remito es requerido");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");

        RuleFor(x => x.RecibidoPor)
            .MaximumLength(200).When(x => x.RecibidoPor is not null)
            .WithMessage("Recibido por no puede superar los 200 caracteres");

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("La devolución debe tener al menos una línea");

        RuleForEach(x => x.Lineas).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductoTerminadoId)
                .NotEmpty().WithMessage("El producto terminado es requerido");

            line.RuleFor(l => l.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");

            line.RuleFor(l => l.Lote)
                .MaximumLength(50).When(l => l.Lote is not null)
                .WithMessage("El lote no puede superar los 50 caracteres");
        });
    }
}