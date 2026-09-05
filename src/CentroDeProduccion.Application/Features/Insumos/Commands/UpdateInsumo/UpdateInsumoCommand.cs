namespace CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;

public sealed record UpdateInsumoCommand(
    Guid Id,
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid UnidadCompraId,
    Guid UnidadConsumoId,
    decimal FactorConversion,
    decimal StockMinimo,
    Guid? ProveedorPrincipalId,
    string? Observaciones,
    byte[] RowVersion,
    decimal Presentacion,
    decimal? PrecioUltimaCompra = null);
