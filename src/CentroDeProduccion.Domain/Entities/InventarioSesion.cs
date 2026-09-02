using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A guided inventory session (toma de inventario) for either insumos or finished
/// products, tracking its conteos and lifecycle state.
/// </summary>
public class InventarioSesion
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public TipoInventario TipoInventario { get; set; }
    public EstadoInventario Estado { get; set; } = EstadoInventario.Abierta;
    public Guid ResponsableId { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding concurrent edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<InventarioConteo> Conteos { get; set; } = new List<InventarioConteo>();
}
