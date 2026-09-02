using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Pagos.Queries;

public sealed record PagoMetodoResponse(MetodoPago Tipo, decimal Monto, string? Referencia);

public sealed record PagoInsumoResponse(
    Guid InsumoId,
    string InsumoNombre,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal);

public sealed record PagoProveedorResponse(
    Guid Id,
    int Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    DateTime FechaPago,
    decimal MontoTotal,
    string? Observaciones,
    IReadOnlyList<PagoMetodoResponse> Metodos,
    IReadOnlyList<PagoInsumoResponse> Insumos);
