using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IMovimientoStockRepository
{
    Task AddAsync(MovimientoStock movimiento, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> GetByInsumoIdAsync(Guid insumoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoStock>> GetByFiltersAsync(
        DateTime from,
        DateTime to,
        Guid? insumoId = null,
        Guid? productoTerminadoId = null,
        TipoMovimientoStock? tipo = null,
        CancellationToken ct = default);
}
