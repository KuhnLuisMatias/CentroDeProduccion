using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Queries;

public sealed record OrdenCompraItemResponse(
    Guid Id,
    Guid InsumoId,
    string InsumoNombre,
    decimal CantidadPedida,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record OrdenCompraResponse(
    Guid Id,
    int Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    EstadoOrdenCompra Estado,
    DateTime FechaCreacion,
    DateTime? FechaEnvio,
    string? Observaciones,
    decimal Total,
    IReadOnlyList<OrdenCompraItemResponse> Items,
    byte[] RowVersion);
