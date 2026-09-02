using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// One output of a production run. Supports the multi-stage yield (spec §18.5): a run can emit
/// a primary product plus a recoverable subproduct ("Recorte") valued separately instead of as
/// merma. Cost is prorated across outputs.
/// </summary>
public class ProduccionSalida
{
    public Guid Id { get; set; }
    public Guid ProduccionId { get; set; }
    public Produccion Produccion { get; set; } = null!;
    public Guid ProductoTerminadoId { get; set; }
    public ProductoTerminado ProductoTerminado { get; set; } = null!;

    /// <summary>Quantity produced in this output.</summary>
    public decimal Cantidad { get; set; }

    /// <summary>Prorated unit cost assigned to this output.</summary>
    public decimal CostoUnitario { get; set; }

    public TipoSalidaProduccion TipoSalida { get; set; } = TipoSalidaProduccion.Primario;
}
