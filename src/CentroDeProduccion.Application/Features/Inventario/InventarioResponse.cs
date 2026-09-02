using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Inventario;

public sealed record CreateInventarioSesionResponse(
    Guid Id,
    TipoInventario Tipo,
    DateTime Fecha,
    EstadoInventario Estado,
    int TotalItems);

public sealed record RegistrarConteoResponse(
    Guid ConteoId,
    decimal CantidadSistema,
    decimal CantidadContada,
    decimal Diferencia,
    bool ConteoOk);

public sealed record ConfirmInventarioSesionResponse(
    Guid SesionId,
    EstadoInventario Estado,
    int AjustesGenerados,
    decimal DiferenciaTotal);

public sealed record InventarioConteoResponse(
    Guid Id,
    Guid? InsumoId,
    string? InsumoNombre,
    Guid? ProductoTerminadoId,
    string? ProductoTerminadoNombre,
    decimal CantidadSistema,
    decimal CantidadContada,
    decimal Diferencia,
    bool ConteoOk,
    string? Observaciones);

public sealed record GetInventarioSesionByIdResponse(
    Guid Id,
    TipoInventario Tipo,
    DateTime Fecha,
    EstadoInventario Estado,
    Guid ResponsableId,
    string? Notas,
    decimal DiferenciaTotal,
    IReadOnlyList<InventarioConteoResponse> Conteos,
    byte[] RowVersion);

public sealed record GetInventarioSesionListResponse(
    Guid Id,
    DateTime Fecha,
    TipoInventario Tipo,
    EstadoInventario Estado,
    int TotalItems,
    decimal DiferenciaTotal,
    byte[] RowVersion);
