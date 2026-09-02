using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.UpdateProductoTerminado;

public sealed record UpdateProductoTerminadoCommand(
    Guid Id,
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid UnidadMedidaId,
    decimal StockMinimo,
    byte[] RowVersion);
