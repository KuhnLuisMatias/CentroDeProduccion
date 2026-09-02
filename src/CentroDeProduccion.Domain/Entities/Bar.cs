using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A bar that receives finished products and inputs from the production center.
/// </summary>
public class Bar
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Encargado { get; set; }
    public string? Telefono { get; set; }
    public string? HorarioRecepcion { get; set; }
    public decimal MargenReventaPorcentaje { get; set; }
    public EstadoBar Estado { get; set; } = EstadoBar.Activo;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding bar edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
