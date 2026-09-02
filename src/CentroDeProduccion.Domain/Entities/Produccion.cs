using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A production run: applying a <see cref="Receta"/> to produce finished-product stock. The lot
/// number is generated at confirmation (not creation) to avoid sequence gaps from cancelled runs.
/// </summary>
public class Produccion
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    /// <summary>Unique sequential lot identifier (spec §5.4), assigned on confirmation.</summary>
    public string Lote { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public Guid ResponsableId { get; set; }
    public Usuario Responsable { get; set; } = null!;
    public EstadoProduccion Estado { get; set; } = EstadoProduccion.Borrador;
    public string? Observaciones { get; set; }

    /// <summary>Total finished units produced across all salidas (set on confirmation).</summary>
    public decimal CantidadProducida { get; set; }

    /// <summary>Expiration date of the produced lot (set on confirmation).</summary>
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>Total insumo cost of the run (set on confirmation).</summary>
    public decimal CostoTotalInsumos { get; set; }

    /// <summary>Total production cost = CostoTotalInsumos (costs are insumos-only since Fase9).</summary>
    public decimal CostoTotal { get; set; }

    /// <summary>Optimistic concurrency token guarding cost recompute.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ProduccionSalida> Salidas { get; set; } = new List<ProduccionSalida>();

    /// <summary>Editable insumo-consumption lines (seeded from the recipe BOM, edited while
    /// Borrador); confirmation deducts exactly these quantities. Kept as a single primary
    /// ProduccionSalida row for report compatibility.</summary>
    public ICollection<ProduccionInsumo> InsumosConsumidos { get; set; } = new List<ProduccionInsumo>();
}
