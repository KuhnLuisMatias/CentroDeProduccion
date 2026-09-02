namespace CentroDeProduccion.Domain.Enums;

// NOTE: `Transferencia` was removed entirely (user decision, not merely rejected at the
// endpoint). It returns only when a future warehouse/location entity exists — until then, a
// transfer recorded as one signed row is indistinguishable from a plain adjustment and would
// permanently poison the ledger's meaning. Operators use AjusteNegativo + AjustePositivo with
// an explicit Motivo instead. See design's "Resolved Open Questions" #2 for the prior
// reject-at-endpoint approach this supersedes.
public enum TipoMovimientoStock
{
    Compra = 1,
    ConsumoProduccion = 2,
    Reventa = 3,
    AjustePositivo = 4,
    AjusteNegativo = 5,
    DevolucionProveedor = 6,
    Produccion = 7,
    VentaBar = 8,
    DevolucionBar = 9,
    BajaPorVencimiento = 10
}
