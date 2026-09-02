namespace CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;

public sealed record CreatePagoProveedorResponse(
    Guid Id,
    int Numero,
    Guid ProveedorId,
    decimal MontoTotal);
