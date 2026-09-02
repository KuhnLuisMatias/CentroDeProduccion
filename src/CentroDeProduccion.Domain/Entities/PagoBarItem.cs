namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A line of a PagoBar allocating part of the payment to a specific remito.
/// </summary>
public class PagoBarItem
{
    public Guid Id { get; set; }
    public Guid PagoBarId { get; set; }
    public PagoBar PagoBar { get; set; } = null!;
    public Guid RemitoId { get; set; }
    public Remito Remito { get; set; } = null!;
    public decimal MontoAplicado { get; set; }
}
