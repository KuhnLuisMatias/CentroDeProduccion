using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Commands.ReserveStock;

public sealed record ReserveStockCommand(Guid ProductoTerminadoId, decimal Cantidad);

public sealed record ReserveStockResponse(
    Guid ProductoTerminadoId,
    EstadoProductoTerminado Estado);
