namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// A sale presentation of a recipe's output (spec §18.7): one production can be sold in N
/// presentations (e.g. porciones 350g / 160g / 900g / 450g) each with its own prorated cost.
/// </summary>
public class PresentacionVenta
{
    public Guid Id { get; set; }
    public Guid RecetaId { get; set; }
    public Receta Receta { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Quantity (in <see cref="UnidadMedidaId"/>) per sale unit.</summary>
    public decimal Cantidad { get; set; }

    public Guid UnidadMedidaId { get; set; }
    public UnidadMedida UnidadMedida { get; set; } = null!;
}
