using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Produccion.Queries.GetProduccionById;

/// <summary>
/// Explicit response contract for GET /api/produccion/{id}. Replaces the raw
/// <c>Produccion</c> entity serialization that leaked <c>Usuario.PasswordHash</c> (via the
/// Responsable navigation) and recursed infinitely (Produccion ↔ Salidas). Property names and
/// shapes mirror frontend/src/lib/types.ts <c>Produccion</c> exactly — do not rename.
/// </summary>
public sealed record GetProduccionByIdResponse(
    Guid Id,
    Guid RecetaId,
    ProduccionRecetaInfo Receta,
    string Lote,
    DateTime Fecha,
    Guid ResponsableId,
    ProduccionResponsableInfo Responsable,
    EstadoProduccion Estado,
    string? Observaciones,
    decimal CantidadProducida,
    DateTime? FechaVencimiento,
    decimal CostoTotalInsumos,
    decimal CostoTotal,
    byte[] RowVersion,
    IReadOnlyList<ProduccionSalidaResponse> Salidas,
    IReadOnlyList<ProduccionInsumoResponse> InsumosConsumidos);

public sealed record ProduccionRecetaInfo(Guid Id, string Nombre, string? UnidadMedidaSimbolo);

/// <summary>Only display fields of the responsible user — never credentials.</summary>
public sealed record ProduccionResponsableInfo(Guid Id, string Nombre, string Apellido);

public sealed record ProduccionSalidaResponse(
    Guid Id,
    Guid ProduccionId,
    Guid ProductoTerminadoId,
    ProduccionSalidaProductoInfo? ProductoTerminado,
    decimal Cantidad,
    TipoSalidaProduccion TipoSalida);

public sealed record ProduccionSalidaProductoInfo(Guid Id, string Nombre, string CodigoSku);

/// <summary>One editable consumption line of the run (Producción simple): either a direct
/// insumo or a sub-recipe consumption whose finished product is deducted at confirm.</summary>
public sealed record ProduccionInsumoResponse(
    Guid Id,
    Guid ProduccionId,
    Guid? InsumoId,
    ProduccionInsumoInsumoInfo? Insumo,
    Guid? RecetaOrigenId,
    ProduccionInsumoRecetaInfo? RecetaOrigen,
    decimal Cantidad,
    /// <summary>Insumo lines: PrecioUltimaCompra; sub-recipe lines: the sub-PT's live standard cost.</summary>
    decimal CostoUnitario,
    string? Observaciones);

public sealed record ProduccionInsumoInsumoInfo(Guid Id, string Nombre, string CodigoSku, Guid UnidadConsumoId);

public sealed record ProduccionInsumoRecetaInfo(Guid Id, string Nombre, string? UnidadMedidaSimbolo);
