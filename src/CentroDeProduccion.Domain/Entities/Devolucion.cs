using CentroDeProduccion.Domain.Services;
namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A return of goods from a bar back to the production center, linked to the
/// original remito it derives from.
/// </summary>
public class Devolucion
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid RemitoId { get; set; }
    public Remito Remito { get; set; } = null!;
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public string? Observaciones { get; set; }
    public string? RecibidoPor { get; set; }
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    public ICollection<DevolucionLinea> Lineas { get; set; } = new List<DevolucionLinea>();
}
