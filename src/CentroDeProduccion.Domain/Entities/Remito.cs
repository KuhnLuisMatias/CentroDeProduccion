using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A delivery of finished products and/or inputs from the production center to a bar.
/// </summary>
public class Remito
{
    public Guid Id { get; set; }
    public int NumeroRemito { get; set; }
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public Guid BarId { get; set; }
    public Bar Bar { get; set; } = null!;
    public EstadoRemito Estado { get; set; } = EstadoRemito.Pendiente;
    public string? Observaciones { get; set; }
    public string? EntregadoPor { get; set; }
    public string? RecibidoPor { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding remito edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RemitoLinea> Lineas { get; set; } = new List<RemitoLinea>();
}
