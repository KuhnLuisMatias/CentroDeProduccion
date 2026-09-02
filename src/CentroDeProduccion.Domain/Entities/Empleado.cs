using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A worker of the production center (spec §8.1). Hourly rate is per-employee so different
/// workers of the same cargo can have different rates. Soft-deleted (Activo=false) rather than
/// removed.
/// </summary>
public class Empleado
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public CargoEmpleado Cargo { get; set; }
    public decimal TarifaPorHora { get; set; }
    public CategoriaEmpleado Categoria { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token guarding employee edits.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
