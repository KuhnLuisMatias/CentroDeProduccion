using CentroDeProduccion.Domain.Services;
namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A payment from a bar, which can be settled through several payment methods and
/// allocated across multiple remitos.
/// </summary>
public class PagoBar
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid BarId { get; set; }
    public Bar Bar { get; set; } = null!;
    public DateTime FechaPago { get; set; }
    public decimal MontoTotal { get; set; }
    public string? Observaciones { get; set; }
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding payment edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<PagoBarMetodo> Metodos { get; set; } = new List<PagoBarMetodo>();
    public ICollection<PagoBarItem> Items { get; set; } = new List<PagoBarItem>();
}
