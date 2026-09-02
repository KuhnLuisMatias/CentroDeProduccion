using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Queries;

public sealed record RemitoLineaResponse(
    Guid Id,
    TipoLineaRemito TipoLinea,
    Guid? ProductoTerminadoId,
    string ProductoTerminadoNombre,
    Guid? InsumoId,
    string InsumoNombre,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    string? Lote,
    string? Observaciones);

public sealed record RemitoResponse(
    Guid Id,
    int NumeroRemito,
    DateTime Fecha,
    Guid BarId,
    string BarNombre,
    string BarDireccion,
    EstadoRemito Estado,
    string? Observaciones,
    string? EntregadoPor,
    string? RecibidoPor,
    DateTime? FechaEnvio,
    decimal Total,
    IReadOnlyList<RemitoLineaResponse> Lineas,
    byte[] RowVersion);

public sealed record RemitoListItemResponse(
    Guid Id,
    int NumeroRemito,
    DateTime Fecha,
    Guid BarId,
    string BarNombre,
    EstadoRemito Estado,
    decimal Total);