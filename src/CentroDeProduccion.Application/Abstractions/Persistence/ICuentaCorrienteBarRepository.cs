using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface ICuentaCorrienteBarRepository
{
    Task AddAsync(CuentaCorrienteBar movimiento, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CuentaCorrienteBar>> GetByBarAsync(
        Guid barId,
        TipoMovimientoCtaCteBar? tipo,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<decimal> GetSaldoAsync(Guid barId, CancellationToken cancellationToken = default);
    Task<decimal> GetDevolucionTotalByRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default);
    Task<decimal> GetDeudaTotalAsync(CancellationToken cancellationToken = default);
}
