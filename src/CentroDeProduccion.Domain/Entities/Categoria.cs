using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

public class Categoria
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Design D8: uniqueness moves from global Nombre to (Ambito, Nombre).</summary>
    public AmbitoCategoria Ambito { get; set; } = AmbitoCategoria.Insumo;

    public bool Activo { get; set; } = true;

    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}
