using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A running account entry for a supplier, recording purchases and payments so the
/// outstanding balance can be derived from the sum of movements.
/// </summary>
public class CuentaCorrienteProveedor
{
    public Guid Id { get; set; }
    public Guid ProveedorId { get; set; }
    public TipoMovimientoCtaCte TipoMovimiento { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public Guid? OrdenCompraId { get; set; }
    public Guid? PagoProveedorId { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    public Proveedor Proveedor { get; set; } = null!;
    public OrdenCompra? OrdenCompra { get; set; }
    public PagoProveedor? PagoProveedor { get; set; }
}
