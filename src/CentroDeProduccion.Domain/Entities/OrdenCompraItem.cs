namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A line of an OrdenCompra referencing an insumo and the agreed quantity and price.
/// </summary>
public class OrdenCompraItem
{
    public Guid Id { get; set; }
    public Guid OrdenCompraId { get; set; }
    public Guid InsumoId { get; set; }
    public decimal CantidadPedida { get; set; }
    public decimal PrecioUnitario { get; set; }

    public OrdenCompra OrdenCompra { get; set; } = null!;
    public Insumo Insumo { get; set; } = null!;
}
