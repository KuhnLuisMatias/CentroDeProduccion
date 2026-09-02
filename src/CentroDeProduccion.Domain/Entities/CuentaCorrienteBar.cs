using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A current-account movement of a bar, tracking outstanding balances derived from
/// remitos, payments, returns and adjustments.
/// </summary>
public class CuentaCorrienteBar
{
    public Guid Id { get; set; }
    public Guid BarId { get; set; }
    public Bar Bar { get; set; } = null!;
    public TipoMovimientoCtaCteBar TipoMovimiento { get; set; }
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
    public Guid? RemitoId { get; set; }
    public Remito? Remito { get; set; }
    public Guid? DevolucionId { get; set; }
    public Devolucion? Devolucion { get; set; }
    public Guid? PagoBarId { get; set; }
    public PagoBar? PagoBar { get; set; }
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;
}
