namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;

public sealed record CreateProductoTerminadoResponse(
    Guid Id,
    string Nombre,
    string CodigoSku);
