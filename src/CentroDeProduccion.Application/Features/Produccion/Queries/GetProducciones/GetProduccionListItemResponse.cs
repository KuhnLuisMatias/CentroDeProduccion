using CentroDeProduccion.Application.Features.Produccion.Queries.GetProduccionById;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Produccion.Queries.GetProducciones;

/// <summary>
/// Explicit response contract for GET /api/produccion. Replaces the raw
/// <c>Produccion</c> entity serialization that leaked <c>Usuario.PasswordHash</c> via the
/// Responsable navigation. Property names mirror frontend/src/lib/types.ts
/// <c>Produccion</c> minus <c>salidas</c> (list endpoint does not include them) — do not rename.
/// </summary>
public sealed record GetProduccionListItemResponse(
    Guid Id,
    Guid RecetaId,
    ProduccionRecetaInfo? Receta,
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
    byte[] RowVersion);
