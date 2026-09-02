namespace CentroDeProduccion.Domain.Enums;

/// <summary>
/// Classifies a <see cref="CentroDeProduccion.Domain.Entities.ProduccionSalida"/> so the
/// multi-stage yield (spec §18.5) can distinguish the primary product from a recoverable
/// subproduct ("Recorte") that is valued separately instead of being recorded as merma.
/// </summary>
public enum TipoSalidaProduccion
{
    Primario = 1,
    Recorte = 2
}
