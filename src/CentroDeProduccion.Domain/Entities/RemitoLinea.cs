using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A line item of a Remito, referencing either a finished product or an input.
/// </summary>
public class RemitoLinea
{
    public Guid Id { get; set; }
    public Guid RemitoId { get; set; }
    public Remito Remito { get; set; } = null!;
    public TipoLineaRemito TipoLinea { get; set; }
    public Guid? ProductoTerminadoId { get; set; }
    public ProductoTerminado? ProductoTerminado { get; set; }
    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public string? Lote { get; set; }
    public string? Observaciones { get; set; }
}
