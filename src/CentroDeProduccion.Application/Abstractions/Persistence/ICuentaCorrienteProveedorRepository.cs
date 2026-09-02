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
}