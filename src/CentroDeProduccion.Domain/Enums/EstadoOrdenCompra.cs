namespace CentroDeProduccion.Domain.Enums;

/// <summary>
/// Lifecycle of a purchase order. The OrdenCompra is a REFERENTIAL document (like a quote or
/// plan): it does not track stock or receptions — the Factura de compra is the real document.
/// </summary>
public enum EstadoOrdenCompra
{
    Borrador = 1,
    Enviada = 2,
    Cancelada = 6
}
