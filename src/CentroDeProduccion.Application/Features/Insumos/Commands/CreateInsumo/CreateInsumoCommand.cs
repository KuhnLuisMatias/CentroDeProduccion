namespace CentroDeProduccion.Application.Features.Insumos.Commands.CreateInsumo;

public sealed record CreateInsumoCommand(
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid UnidadCompraId,
    Guid UnidadConsumoId,
    decimal FactorConversion,
    decimal StockMinimo,
    Guid? ProveedorPrincipalId,
    string? Observaciones,
    decimal? PrecioUltimaCompra = null);
