using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

public class MovimientoStock
{
    public Guid Id { get; set; }

    /// <summary>Target insumo (nullable). Exactly one of <see cref="InsumoId"/> /
    /// <see cref="ProductoTerminadoId"/> must be set (enforced by CHECK constraint).</summary>
    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    /// <summary>Target finished product (nullable) for product-terminado movements.</summary>
    public Guid? ProductoTerminadoId { get; set; }
    public ProductoTerminado? ProductoTerminado { get; set; }

    /// <summary>Reference to the originating production run, when applicable.</summary>
    public Guid? ProduccionId { get; set; }
    public Produccion? Produccion { get; set; }

    public TipoMovimientoStock Tipo { get; set; }

    /// <summary>Signed, always in the insumo's consumption unit (design D6/D7) — the only
    /// column the ledger sums.</summary>
    public decimal Cantidad { get; set; } // positivo = suma, negativo = resta

    /// <summary>Unsigned, as entered by the caller in <see cref="UnidadOriginalId"/>.</summary>
    public decimal CantidadOriginal { get; set; }

    /// <summary>The unit the operator entered the quantity in (purchase or consumption unit).</summary>
    public Guid UnidadOriginalId { get; set; }
    public UnidadMedida UnidadOriginal { get; set; } = null!;

    /// <summary>Snapshot of Insumo.FactorConversion at write time — a later factor correction
    /// cannot retroactively alter history.</summary>
    public decimal FactorConversionAplicado { get; set; }

    /// <summary>Per purchase unit; only set for <see cref="TipoMovimientoStock.Compra"/>.</summary>
    public decimal? PrecioUnitario { get; set; }

    public string Motivo { get; set; } = string.Empty;
    public string? DocumentoOrigen { get; set; } // referencia: número de compra, id de producción, etc.
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime Fecha { get; set; } = RelojDeNegocio.Ahora;
}
