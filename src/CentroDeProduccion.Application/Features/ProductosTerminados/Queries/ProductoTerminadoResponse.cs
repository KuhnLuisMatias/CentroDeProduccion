using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.ProductosTerminados.Queries;

/// <summary>
/// Explicit response contract for GET /api/productoterminado endpoints. Keeps the same JSON
/// shape the frontend expects (frontend/src/lib/types.ts <c>ProductoTerminado</c>) but
/// CostoUnitario is computed on the fly from the recipe BOM at current insumo prices —
/// never stored. Do not rename.
/// </summary>
public sealed record ProductoTerminadoResponse(
    Guid Id,
    string Nombre,
    string CodigoSku,
    Guid CategoriaId,
    Guid UnidadMedidaId,
    decimal StockActual,
    decimal StockMinimo,
    decimal CostoUnitario,
    DateTime FechaProduccion,
    DateTime FechaVencimiento,
    string Lote,
    EstadoProductoTerminado Estado,
    bool Activo,
    DateTime FechaCreacion,
    ProductoTerminadoCategoriaInfo? Categoria,
    ProductoTerminadoUnidadInfo? UnidadMedida,
    Guid? RecetaId,
    ProductoTerminadoRecetaInfo? Receta);

public sealed record ProductoTerminadoCategoriaInfo(Guid Id, string Nombre);

public sealed record ProductoTerminadoUnidadInfo(Guid Id, string Nombre, string Simbolo);

public sealed record ProductoTerminadoRecetaInfo(Guid Id, string Nombre);
