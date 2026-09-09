namespace CentroDeProduccion.Domain.Enums;

/// <summary>
/// Destino de una línea devuelta por un bar: define si reingresa al stock o no.
/// Solo ReingresoStock mueve stock y genera crédito en cuenta corriente.
/// </summary>
public enum DestinoDevolucion
{
    ReingresoStock = 1,
    Cuarentena = 2,
    MalEstado = 3
}
