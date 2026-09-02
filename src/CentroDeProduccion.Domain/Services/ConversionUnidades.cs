namespace CentroDeProduccion.Domain.Services;

/// <summary>
/// The sole multiplication site converting a quantity entered in either an insumo's purchase
/// unit or its consumption unit into the canonical consumption-unit quantity (design D6).
/// Contract: 1 purchase unit = <c>factorConversion</c> consumption units. Direction is derived
/// from which of the insumo's two configured unit ids the caller passes — never asserted by
/// the caller — so a direction inversion is structurally unrepresentable at the API boundary.
/// </summary>
public static class ConversionUnidades
{
    /// <summary>
    /// Converts <paramref name="cantidad"/> (expressed in <paramref name="unidadIngresadaId"/>)
    /// into the equivalent quantity in the insumo's consumption unit.
    /// </summary>
    /// <param name="cantidad">Unsigned quantity as entered by the caller.</param>
    /// <param name="unidadIngresadaId">The unit id the caller entered the quantity in.</param>
    /// <param name="unidadCompraId">The insumo's configured purchase unit id.</param>
    /// <param name="unidadConsumoId">The insumo's configured consumption unit id.</param>
    /// <param name="factorConversion">1 purchase unit = this many consumption units. Must be &gt; 0.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factorConversion"/> is not positive.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="unidadIngresadaId"/> is neither <paramref name="unidadCompraId"/> nor
    /// <paramref name="unidadConsumoId"/>.
    /// </exception>
    public static decimal ToUnidadConsumo(
        decimal cantidad,
        Guid unidadIngresadaId,
        Guid unidadCompraId,
        Guid unidadConsumoId,
        decimal factorConversion)
    {
        if (factorConversion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factorConversion), factorConversion, "FactorConversion must be greater than zero.");
        }

        if (unidadIngresadaId == unidadCompraId)
        {
            return cantidad * factorConversion;
        }

        if (unidadIngresadaId == unidadConsumoId)
        {
            return cantidad;
        }

        throw new ArgumentException(
            "unidadIngresadaId must be either the insumo's UnidadCompraId or UnidadConsumoId.",
            nameof(unidadIngresadaId));
    }
}
