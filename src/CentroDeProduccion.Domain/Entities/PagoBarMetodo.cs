using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A payment method line of a PagoBar (owned value object).
/// </summary>
public class PagoBarMetodo
{
    public MetodoPago Tipo { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
