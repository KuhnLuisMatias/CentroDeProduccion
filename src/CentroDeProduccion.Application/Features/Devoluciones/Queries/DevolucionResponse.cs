namespace CentroDeProduccion.Application.Features.Devoluciones.Queries;

using CentroDeProduccion.Domain.Enums;

public sealed record DevolucionLineaResponse(
    Guid Id,
    TipoLineaRemito TipoLinea,
    Guid? ProductoTerminadoId,
    string ProductoTerminadoNombre,
    Guid? InsumoId,
    string InsumoNombre,
    decimal Cantidad,
    string? Lote,
    DestinoDevolucion Destino,
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