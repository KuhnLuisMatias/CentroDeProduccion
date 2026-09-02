namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// One line of a recipe: either a direct insumo (<see cref="InsumoId"/>) or a sub-recipe
/// (<see cref="RecetaOrigenId"/>) for BOM recursion (spec §18.6). Exactly one of the two must
/// be set; the other stays null.
/// </summary>
public class RecetaInsumo
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;

    public Guid? InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    public Guid? RecetaOrigenId { get; set; }
    public Receta? RecetaOrigen { get; set; }

    /// <summary>Quantity of the insumo (or sub-recipe units) used for the full recipe yield.</summary>
    public decimal CantidadNecesaria { get; set; }

    public Guid UnidadMedidaId { get; set; }
    public UnidadMedida UnidadMedida { get; set; } = null!;

    public string? Observaciones { get; set; }
}
