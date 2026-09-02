namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A line item of a Devolucion, referencing the returned finished product.
/// </summary>
public class DevolucionLinea
{
    public Guid Id { get; set; }
    public Guid DevolucionId { get; set; }
    public Devolucion Devolucion { get; set; } = null!;
    public Guid ProductoTerminadoId { get; set; }
    public ProductoTerminado ProductoTerminado { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public string? Lote { get; set; }
}
