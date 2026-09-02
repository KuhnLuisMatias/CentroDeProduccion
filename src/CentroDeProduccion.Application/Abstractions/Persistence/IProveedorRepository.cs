using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IProveedorRepository
{
    Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithCuitAsync(string cuit, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Proveedor>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Proveedor>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default);
}
