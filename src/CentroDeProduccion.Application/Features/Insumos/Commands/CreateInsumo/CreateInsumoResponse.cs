namespace CentroDeProduccion.Application.Features.Insumos.Commands.CreateInsumo;

public sealed record CreateInsumoResponse(
    Guid Id,
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    string CategoriaNombre,
    Guid UnidadCompraId,
    string UnidadCompraSimbolo,
    Guid UnidadConsumoId,
    string UnidadConsumoSimbolo,
    decimal FactorConversion,
    decimal Presentacion,
    decimal StockMinimo,
    decimal StockActual,
    decimal PrecioUltimaCompra,
    Guid? ProveedorPrincipalId,
    string? Observaciones);
