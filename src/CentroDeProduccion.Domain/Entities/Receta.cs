using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// Defines how a finished product is produced: which insumos (or sub-recipes) are consumed
/// and in what quantity. Costing is always the batch total of insumos at last purchase price,
/// resolved recursively through <see cref="Services.CostoService"/> when a recipe consumes
/// another recipe (BOM, spec §18.6).
/// </summary>
public class Receta
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoSku { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    public string? Descripcion { get; set; }

    /// <summary>Unit in which the recipe's OUTPUT is measured (nullable for legacy rows).</summary>
    public Guid? UnidadMedidaId { get; set; }
    public UnidadMedida? UnidadMedida { get; set; }

    public EstadoReceta Estado { get; set; } = EstadoReceta.Activa;

    /// <summary>Monotonic version number; incremented on every modification (spec §3.5).</summary>
    public int Version { get; set; } = 1;

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding recipe edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RecetaInsumo> Insumos { get; set; } = new List<RecetaInsumo>();
    public ICollection<PresentacionVenta> Presentaciones { get; set; } = new List<PresentacionVenta>();
    public ICollection<RecetaVersion> Versiones { get; set; } = new List<RecetaVersion>();
}
