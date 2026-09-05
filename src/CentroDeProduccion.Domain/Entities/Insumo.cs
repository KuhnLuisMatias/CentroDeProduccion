using CentroDeProduccion.Domain.Services;
namespace CentroDeProduccion.Domain.Entities;

public class Insumo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoSku { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    // Design D6: free-text UnidadMedida replaced by a reference-entity pair.
    // Contract: 1 UnidadCompra = FactorConversion UnidadConsumo.
    public Guid UnidadCompraId { get; set; }
    public UnidadMedida UnidadCompra { get; set; } = null!;
    public Guid UnidadConsumoId { get; set; }
    public UnidadMedida UnidadConsumo { get; set; } = null!;
    public decimal FactorConversion { get; set; } = 1;

    /// <summary>Contenido de 1 unidad de compra expresado en la unidad de consumo
    /// (ej. bidón de 5 litros: UnidadCompra=Bidón, UnidadConsumo=Litro, Presentacion=5).
    /// Es el valor visible/editable; <see cref="FactorConversion"/> se sincroniza con él
    /// al guardar para que la conversión de stock existente siga funcionando.</summary>
    public decimal Presentacion { get; set; } = 1;

    public decimal StockMinimo { get; set; }
    public decimal StockActual { get; set; }
    public decimal PrecioUltimaCompra { get; set; }

    public Guid? ProveedorPrincipalId { get; set; }
    public Proveedor? ProveedorPrincipal { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = RelojDeNegocio.Ahora;

    /// <summary>Optimistic concurrency token (design D7): guards the ledger write path
    /// against lost updates when two movements race on the same insumo.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
}
