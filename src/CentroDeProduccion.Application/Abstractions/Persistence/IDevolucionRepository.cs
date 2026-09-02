using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IDevolucionRepository
{
    Task<Devolucion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Devolucion?> GetByIdWithLineasAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Devolucion>> GetByFiltersAsync(
        Guid? remitoId,
        Guid? barId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalDevueltoForLineAsync(Guid remitoId, Guid productoTerminadoId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, decimal>> GetTotalesDevueltosPorRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default);
    Task AddAsync(Devolucion devolucion, CancellationToken cancellationToken = default);
}
