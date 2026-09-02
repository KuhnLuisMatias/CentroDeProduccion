using CentroDeProduccion.Domain.Enums;
using FluentValidation;

namespace CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;

public class RegisterMovementCommandValidator : AbstractValidator<RegisterMovementCommand>
{
    private static readonly TipoMovimientoStock[] InsumoOnlyTypes =
    {
        TipoMovimientoStock.Compra,
        TipoMovimientoStock.DevolucionProveedor,
        TipoMovimientoStock.ConsumoProduccion,
        TipoMovimientoStock.Reventa
    };

    private static readonly TipoMovimientoStock[] ProductoOnlyTypes =
    {
        TipoMovimientoStock.Produccion,
        TipoMovimientoStock.VentaBar,
        TipoMovimientoStock.DevolucionBar,
        TipoMovimientoStock.BajaPorVencimiento
    };

    // Valid for BOTH insumo and finished-product targets (spec §4.2, §5.3).
    private static readonly TipoMovimientoStock[] AmbosTypes =
    {
        TipoMovimientoStock.AjustePositivo,
        TipoMovimientoStock.AjusteNegativo
    };

    public RegisterMovementCommandValidator()
    {
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("Tipo de movimiento no válido");

        RuleFor(x => x)
            .Must(x => (x.InsumoId.HasValue) != (x.ProductoTerminadoId.HasValue))
            .WithMessage("Debe indicar exactamente un insumo O un producto terminado");

        RuleFor(x => x.Tipo)
            .Must(t => InsumoOnlyTypes.Contains(t) || ProductoOnlyTypes.Contains(t) || AmbosTypes.Contains(t))
            .WithMessage("Tipo de movimiento no soportado");

        RuleFor(x => x.InsumoId)
            .NotNull().When(x => InsumoOnlyTypes.Contains(x.Tipo))
            .WithMessage("Este tipo de movimiento requiere un insumo");

        RuleFor(x => x.ProductoTerminadoId)
            .NotNull().When(x => ProductoOnlyTypes.Contains(x.Tipo))
            .WithMessage("Este tipo de movimiento requiere un producto terminado");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero");

        RuleFor(x => x.UnidadOriginalId)
            .NotEmpty().WithMessage("La unidad de medida es requerida");

        RuleFor(x => x.PrecioUnitario)
            .NotNull().When(x => x.Tipo == TipoMovimientoStock.Compra)
            .WithMessage("El precio unitario es requerido para compras");

        RuleFor(x => x.PrecioUnitario)
            .GreaterThan(0).When(x => x.Tipo == TipoMovimientoStock.Compra && x.PrecioUnitario.HasValue)
            .WithMessage("El precio unitario debe ser mayor a cero");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo es requerido")
            .MaximumLength(500);
    }
}
