using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;

public sealed record CreateOrdenCompraResponse(
    Guid Id,
    int Numero,
    Guid ProveedorId,
    EstadoOrdenCompra Estado,
    decimal Total);