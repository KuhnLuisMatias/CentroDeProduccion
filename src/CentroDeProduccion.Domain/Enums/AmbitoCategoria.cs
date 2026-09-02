namespace CentroDeProduccion.Domain.Enums;

/// <summary>
/// Scopes a <c>Categoria</c> so the same name can exist independently in each scope (design
/// D8): the unique index moves from global <c>Nombre</c> to <c>(Ambito, Nombre)</c>.
/// </summary>
public enum AmbitoCategoria
{
    Insumo = 1,
    ProductoTerminado = 2
}
