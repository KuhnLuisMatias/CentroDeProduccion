using CentroDeProduccion.Domain.Services;
namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A supplier purchase invoice ("Factura de Compra"): the real purchase document that sums
/// insumo stock (via Compra movements) and generates supplier debt in cuenta corriente.
/// Settled through one or several payment methods. The Orden de Compra is referential only.
/// </summary>
public class PagoProveedor
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid ProveedorId { get; set; }
    public DateTime FechaPago { get; set; }
    public decimal MontoTotal { get; set; }
    public string? Observaciones { get; set; }
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    public ICollection<PagoMetodo> Metodos { get; set; } = new List<PagoMetodo>();
    public ICollection<PagoInsumo> Insumos { get; set; } = new List<PagoInsumo>();

    public Proveedor Proveedor { get; set; } = null!;
}
