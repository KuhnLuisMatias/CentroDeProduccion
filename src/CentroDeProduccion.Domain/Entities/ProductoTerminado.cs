using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A finished-product stock entry with lot and expiration (spec §5.2). Produced by a
/// <see cref="Produccion"/> run; consumed by remitos to bares (Phase 5).
/// </summary>
public class ProductoTerminado
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoSku { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public Guid UnidadMedidaId { get; set; }
    public UnidadMedida UnidadMedida { get; set; } = null!;

    public decimal StockActual { get; set; }

    /// <summary>Minimum stock level that triggers a restock alert.</summary>
    public decimal StockMinimo { get; set; }

    /// <summary>Recipe this finished product derives from (producción simple). Nullable:
    /// manually-created finished products have no recipe. Cost is NOT stored — it is computed
    /// on the fly from the recipe's BOM at current insumo prices (see
    /// Application/Common/ProductoTerminadoCostoResolver).</summary>
    public Guid? RecetaId { get; set; }
    public Receta? Receta { get; set; }

    public DateTime FechaProduccion { get; set; }
    public DateTime FechaVencimiento { get; set; }

    /// <summary>Unique lot identifier produced per production run (spec §5.4).</summary>
    public string Lote { get; set; } = string.Empty;

    public EstadoProductoTerminado Estado { get; set; } = EstadoProductoTerminado.Disponible;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding finished-product stock edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
