using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegistrarPagoProveedor;

public class RegistrarPagoProveedorCommandValidator : AbstractValidator<RegistrarPagoProveedorCommand>
{
    public RegistrarPagoProveedorCommandValidator()
    {
        RuleFor(x => x.ProveedorId)
            .NotEmpty().WithMessage("El proveedor es requerido");
        RuleFor(x => x.FacturaId)
            .NotEmpty().When(x => x.FacturaId.HasValue)
            .WithMessage("El identificador de la factura no puede ser vacío");
        RuleFor(x => x.Fecha)
            .Must(fecha => fecha.Date <= RelojDeNegocio.Ahora.Date)
            .WithMessage("La fecha del pago no puede ser futura");
        RuleFor(x => x.Observaciones)
            .MaximumLength(500).When(x => x.Observaciones is not null)
            .WithMessage("Las observaciones no pueden superar los 500 caracteres");
        RuleFor(x => x.Medios)
            .NotEmpty().WithMessage("Debe indicar al menos un medio de pago");
        RuleForEach(x => x.Medios).ChildRules(metodo =>
        {
            metodo.RuleFor(m => m.Monto)
                .GreaterThan(0).WithMessage("El monto de cada medio de pago debe ser mayor a cero");
            metodo.RuleFor(m => m.Tipo)
                .Must(tipo => Enum.IsDefined(typeof(MetodoPago), tipo))
                .WithMessage("El medio de pago indicado no es válido");
            metodo.RuleFor(m => m.Referencia)
                .MaximumLength(100).When(m => m.Referencia is not null)
                .WithMessage("La referencia no puede superar los 100 caracteres");
        });
        RuleFor(x => x.Medios)
            .Must(medios => medios.Sum(m => m.Monto) > 0)
            .WithMessage("El total del pago debe ser mayor a cero");
    }
}
