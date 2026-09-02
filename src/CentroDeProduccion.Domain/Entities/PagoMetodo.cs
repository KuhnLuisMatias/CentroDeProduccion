using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A payment method line of a PagoProveedor (owned value object). Referencia holds free
/// text such as the cheque number when Tipo is Cheque (no Cheque entity).
/// </summary>
public class PagoMetodo
{
    public MetodoPago Tipo { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
