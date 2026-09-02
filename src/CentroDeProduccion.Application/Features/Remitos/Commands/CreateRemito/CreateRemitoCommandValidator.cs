using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;

public class CreateRemitoCommandValidator : AbstractValidator<CreateRemitoCommand>
{
    public CreateRemitoCommandValidator()
    {
        RuleFor(x => x.BarId)
            .NotEmpty().WithMessage("El bar es requerido");

        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");

        RuleFor(x => x.EntregadoPor)
            .MaximumLength(200).When(x => x.EntregadoPor is not null)
            .WithMessage("Entregado por no puede superar los 200 caracteres");

        RuleFor(x => x.RecibidoPor)
            .MaximumLength(200).When(x => x.RecibidoPor is not null)
            .WithMessage("Recibido por no puede superar los 200 caracteres");

        RuleFor(x => x.Lineas)
            .NotEmpty().WithMessage("El remito debe tener al menos una línea");

        RuleForEach(x => x.Lineas).ChildRules(line =>
        {
            line.RuleFor(l => l.TipoLinea)
                .IsInEnum().WithMessage("El tipo de línea no es válido");

            line.When(l => l.TipoLinea == TipoLineaRemito.ProductoTerminado, () =>
            {
                line.RuleFor(l => l.ProductoTerminadoId)
                    .NotEmpty().WithMessage("El producto terminado es requerido");
                line.RuleFor(l => l.InsumoId)
                    .Empty().WithMessage("Un producto terminado no puede referenciar un insumo");
            });

            line.When(l => l.TipoLinea == TipoLineaRemito.Insumo, () =>
            {
                line.RuleFor(l => l.InsumoId)
                    .NotEmpty().WithMessage("El insumo es requerido");
                line.RuleFor(l => l.ProductoTerminadoId)
                    .Empty().WithMessage("Un insumo no puede referenciar un producto terminado");
            });

            line.RuleFor(l => l.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");

            line.RuleFor(l => l.Lote)
                .MaximumLength(50).When(l => l.Lote is not null)
                .WithMessage("El lote no puede superar los 50 caracteres");

            line.RuleFor(l => l.Observaciones)
                .MaximumLength(500).When(l => l.Observaciones is not null)
                .WithMessage("Las observaciones no pueden superar los 500 caracteres");
        });
    }
}