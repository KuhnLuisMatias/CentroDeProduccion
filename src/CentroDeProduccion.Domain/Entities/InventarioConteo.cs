namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A single stock count line within an <see cref="InventarioSesion"/>, targeting
/// either an insumo or a finished product (XOR).
/// </summary>
public class InventarioConteo
{
    public Guid Id { get; set; }
    public Guid InventarioSesionId { get; set; }
    public InventarioSesion InventarioSesion { get; set; } = null!;
    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public Guid? ProductoTerminadoId { get; set; }
    public ProductoTerminado? ProductoTerminado { get; set; }
    public decimal CantidadSistema { get; set; }
    public decimal CantidadContada { get; set; }
    public string? Observaciones { get; set; }

    public decimal Diferencia => CantidadContada - CantidadSistema;
    public bool ConteoOk => Diferencia == 0;
}
