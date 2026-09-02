using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IInventarioSesionRepository
{
    Task<Guid> AddAsync(InventarioSesion session, CancellationToken ct = default);
    Task<InventarioSesion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventarioSesion?> GetByIdWithConteosAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<InventarioSesion>> GetAllAsync(CancellationToken ct = default);
    Task AddConteoAsync(InventarioConteo conteo, CancellationToken ct = default);
}
