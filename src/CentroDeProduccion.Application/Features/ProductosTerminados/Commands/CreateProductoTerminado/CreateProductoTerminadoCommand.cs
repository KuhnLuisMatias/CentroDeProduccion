using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;

public sealed record CreateProductoTerminadoCommand(
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid UnidadMedidaId);
