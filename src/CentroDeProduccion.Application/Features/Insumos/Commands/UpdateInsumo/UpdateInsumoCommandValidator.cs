using FluentValidation;

namespace CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;

public class UpdateInsumoCommandValidator : AbstractValidator<UpdateInsumoCommand>
{
    public UpdateInsumoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID es requerido");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.CodigoSku)
            .NotEmpty().WithMessage("El código SKU es requerido")
            .MaximumLength(50).WithMessage("El SKU no puede exceder 50 caracteres");

        RuleFor(x => x.CategoriaId)
            .NotEmpty().WithMessage("La categoría es requerida");

        RuleFor(x => x.UnidadCompraId)
            .NotEmpty().WithMessage("La unidad de compra es requerida");

        RuleFor(x => x.UnidadConsumoId)
            .NotEmpty().WithMessage("La unidad de consumo es requerida");

        RuleFor(x => x.FactorConversion)
            .GreaterThan(0).WithMessage("El factor de conversión debe ser mayor a 0");

        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo");

        RuleFor(x => x.PrecioUltimaCompra)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de última compra no puede ser negativo")
            .When(x => x.PrecioUltimaCompra.HasValue);

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("El RowVersion es requerido para concurrencia optimista");
    }
}
