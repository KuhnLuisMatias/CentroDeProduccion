using FluentValidation;

namespace CentroDeProduccion.Application.Features.Proveedores.Commands.CreateProveedor;

public class CreateProveedorCommandValidator : AbstractValidator<CreateProveedorCommand>
{
    public CreateProveedorCommandValidator()
    {
        RuleFor(x => x.NombreRazonSocial)
            .NotEmpty().WithMessage("El nombre/razón social es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("El CUIT es requerido")
            .Matches(@"^\d{2}-\d{8}-\d$").WithMessage("El CUIT debe tener formato XX-XXXXXXXX-X");

        RuleFor(x => x.Direccion)
            .NotEmpty().WithMessage("La dirección es requerida")
            .MaximumLength(300).WithMessage("La dirección no puede exceder 300 caracteres");

        RuleFor(x => x.TipoFactura)
            .NotEmpty().WithMessage("El tipo de factura es requerido")
            .Must(t => new[] { "A", "B", "C" }.Contains(t))
            .WithMessage("El tipo de factura debe ser A, B o C");

        RuleFor(x => x.CategoriasProvee)
            .NotEmpty().WithMessage("Las categorías que provee son requeridas");
    }
}
