using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A purchase order sent to a supplier. It is a referential document: it does NOT affect stock
/// or record receptions (the Factura de compra does that). Tracks Borrador → Enviada → Cancelada.
/// </summary>
public class OrdenCompra
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid ProveedorId { get; set; }
    public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.Borrador;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;
    public DateTime? FechaEnvio { get; set; }
    public string? Observaciones { get; set; }
    public Guid CreadoPor { get; set; }

    /// <summary>Optimistic concurrency token guarding purchase order edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<OrdenCompraItem> Items { get; set; } = new List<OrdenCompraItem>();

    public Proveedor Proveedor { get; set; } = null!;
}
