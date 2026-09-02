namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// One consumed-insumo line of a production run. Seeded from the recipe BOM at creation
/// (Producción simple flow) and freely editable by the operator while the run is in Borrador;
/// the final list is what confirmation deducts from stock.
/// </summary>
public class ProduccionInsumo
{
    public Guid Id { get; set; }
    public Guid ProduccionId { get; set; }
    public Produccion Produccion { get; set; } = null!;
    public Guid InsumoId { get; set; }
    public Insumo Insumo { get; set; } = null!;

    /// <summary>Quantity to consume, in the insumo's consumption unit.</summary>
    public decimal Cantidad { get; set; }

    public string? Observaciones { get; set; }
}
