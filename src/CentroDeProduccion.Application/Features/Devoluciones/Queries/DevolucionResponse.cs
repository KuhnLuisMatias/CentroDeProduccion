namespace CentroDeProduccion.Application.Features.Devoluciones.Queries;

public sealed record DevolucionLineaResponse(
    Guid Id,
    string ProductoTerminadoNombre,
    decimal Cantidad,
    string? Lote,
    decimal PrecioUnitarioOriginal,
    decimal Subtotal);

public sealed record DevolucionResponse(
    Guid Id,
    int Numero,
    Guid RemitoId,
    int RemitoNumeroRemito,
    DateTime Fecha,
    string? Observaciones,
    string? RecibidoPor,
    Guid BarId,
    string BarNombre,
    decimal TotalDevolucion,
    IReadOnlyList<DevolucionLineaResponse> Lineas);

public sealed record DevolucionListItemResponse(
    Guid Id,
    int Numero,
    Guid RemitoId,
    int RemitoNumeroRemito,
    Guid BarId,
    string BarNombre,
    DateTime Fecha,
    decimal Total);