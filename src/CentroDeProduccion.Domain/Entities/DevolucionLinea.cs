namespace CentroDeProduccion.Domain.Entities;

using CentroDeProduccion.Domain.Enums;

/// <summary>
/// A line item of a Devolucion: either a returned finished product
/// (<see cref="ProductoTerminadoId"/>) or a returned insumo (<see cref="InsumoId"/>).
/// Exactly one of the two must be set. <see cref="Destino"/> decides whether the
/// quantity goes back to stock (only <see cref="DestinoDevolucion.ReingresoStock"/> does).
/// </summary>
public class DevolucionLinea
{
    public Guid Id { get; set; }
    public Guid DevolucionId { get; set; }
    public Devolucion Devolucion { get; set; } = null!;
    public Guid? ProductoTerminadoId { get; set; }
    public ProductoTerminado? ProductoTerminado { get; set; }
    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }
    public decimal Cantidad { get; set; }
    public string? Lote { get; set; }
    public DestinoDevolucion Destino { get; set; } = DestinoDevolucion.ReingresoStock;
}
