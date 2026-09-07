using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface ICuentaCorrienteProveedorRepository
{
    Task AddAsync(CuentaCorrienteProveedor movimiento, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CuentaCorrienteProveedor>> GetByProveedorAsync(
        Guid proveedorId,
        TipoMovimientoCtaCte? tipo,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<decimal> GetSaldoAsync(Guid proveedorId, CancellationToken cancellationToken = default);
    Task<decimal> GetDeudaTotalAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, decimal>> GetSaldosPorProveedorAsync(CancellationToken ct = default);

    /// <summary>Total settled against one factura de compra as a POSITIVE decimal: Σ of the
    /// negated negative CC rows linked to it (Pago movements; the Compra row also carries
    /// PagoProveedorId but is positive).</summary>
    Task<decimal> GetPagosAplicadosAFacturaAsync(Guid facturaId, CancellationToken cancellationToken = default);

    /// <summary>Batched per-factura settled totals (positive decimals) for the given facturas
    /// (missing ids → 0).</summary>
    Task<Dictionary<Guid, decimal>> GetPagosAplicadosPorFacturaAsync(
        IReadOnlyCollection<Guid> facturaIds, CancellationToken cancellationToken = default);
}