using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// Immutable snapshot of a recipe at a given version (spec §3.5). Created whenever a recipe is
/// modified, so the previous definition (and its insumos) remains recoverable. The insumo
/// detail is serialized as JSON rather than a relational copy to keep the snapshot self-contained.
/// </summary>
public class RecetaVersion
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    public int Version { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoSku { get; set; } = string.Empty;

    /// <summary>JSON serialization of the recipe's insumo lines at this version.</summary>
    public string DetallesJson { get; set; } = "[]";

    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;
}
