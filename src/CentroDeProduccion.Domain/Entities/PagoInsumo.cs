using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// An insumo line of a PagoProveedor ("Factura de Compra", owned value object). Each line
/// generates a Compra stock movement and contributes its subtotal to the supplier debt.
/// </summary>
public class PagoInsumo
{
    public Guid Id { get; set; }
    public Guid InsumoId { get; set; }
    public Insumo Insumo { get; set; } = null!;

    /// <summary>Quantity in the insumo's purchase unit.</summary>
    public decimal Cantidad { get; set; }

    /// <summary>Price per purchase unit.</summary>
    public decimal PrecioUnitario { get; set; }

    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;
}
