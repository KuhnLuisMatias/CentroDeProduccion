namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// One consumption line of a production run. Seeded from the recipe BOM at creation
/// (Producción simple flow) and freely editable by the operator while the run is in Borrador;
/// the final list is what confirmation deducts from stock. Each line is either a direct
/// insumo (<see cref="InsumoId"/>) or a sub-recipe consumption (<see cref="RecetaOrigenId"/>,
/// deducted at confirm from the finished product whose RecetaId matches that sub-recipe).
/// Exactly one of the two must be set; the other stays null.
/// </summary>
public class ProduccionInsumo
{
    public Guid Id { get; set; }
    public Guid ProduccionId { get; set; }
    public Produccion Produccion { get; set; } = null!;

    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    /// <summary>Sub-recipe whose finished product gets consumed at confirmation (mirrors
    /// <see cref="RecetaInsumo.RecetaOrigenId"/>).</summary>
    public Guid? RecetaOrigenId { get; set; }
    public Receta? RecetaOrigen { get; set; }

    /// <summary>Quantity to consume, in the insumo's consumption unit.</summary>
    public decimal Cantidad { get; set; }

    public string? Observaciones { get; set; }
}
