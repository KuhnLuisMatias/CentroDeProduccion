using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A payment made to a supplier ("pago a proveedor"): settles the debt of one factura de compra
/// (fully or partially, referenced by PagoAProveedor.FacturaId) or pays on account. Append-only:
/// it writes one negative CuentaCorrienteProveedor row (TipoMovimiento.Pago); the settled amount
/// per factura is derived from the ledger, never stored on the invoice.
/// </summary>
public class PagoAProveedor
{
    public Guid Id { get; set; }
    public Guid ProveedorId { get; set; }

    /// <summary>The payment date, supplied by the client.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Computed server-side as the sum of the payment method lines.</summary>
    public decimal MontoTotal { get; set; }

    public string? Observaciones { get; set; }
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Factura de compra being settled (null = payment on account).</summary>
    public Guid? FacturaId { get; set; }

    public PagoProveedor? Factura { get; set; }

    public Proveedor Proveedor { get; set; } = null!;

    public ICollection<PagoAProveedorMetodo> Medios { get; set; } = new List<PagoAProveedorMetodo>();
}

/// <summary>A payment method line of a PagoAProveedor (owned value object).</summary>
public class PagoAProveedorMetodo
{
    public Guid Id { get; set; }
    public MetodoPago Tipo { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}
