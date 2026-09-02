namespace CentroDeProduccion.Domain.Enums;

/// <summary>
/// Classifies a <c>UnidadMedida</c> so validators can decide integrality rules (design,
/// resolved open question #1): only <see cref="Conteo"/> units require an integral quantity
/// when *entered*; storage stays <c>decimal(18,4)</c> for every type because a converted value
/// (e.g. Cj → Uni) can legitimately be fractional downstream.
/// </summary>
public enum TipoUnidadMedida
{
    Masa = 1,
    Volumen = 2,
    Conteo = 3
}
