namespace CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;

public sealed record PagoInsumoCommand(
    Guid InsumoId,
    decimal Cantidad,
    decimal PrecioUnitario);

public sealed record CreatePagoProveedorCommand(
    Guid ProveedorId,
    DateTime FechaPago,
    decimal MontoTotal,
    string? Observaciones,
    IReadOnlyList<PagoInsumoCommand> Insumos);
