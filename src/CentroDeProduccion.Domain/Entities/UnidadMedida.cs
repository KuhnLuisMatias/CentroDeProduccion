using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Domain.Entities;

/// <summary>
/// Reference entity replacing the free-text <c>Insumo.UnidadMedida</c> string (design D6).
/// <c>Insumo</c> references two rows here: <c>UnidadCompraId</c> and <c>UnidadConsumoId</c>.
/// </summary>
public class UnidadMedida
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Simbolo { get; set; } = string.Empty;
    public TipoUnidadMedida Tipo { get; set; }
    public bool Activo { get; set; } = true;
}
