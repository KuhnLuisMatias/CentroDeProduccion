using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IInsumoRepository
{
    Task<Insumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Insumo insumo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Insumo>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<int> GetCriticosCountAsync(CancellationToken cancellationToken = default);
    Task<Insumo?> GetByIdWithMovementsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Insumo>> GetPagedAsync(string? searchTerm, int page, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Insumo>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<(int Total, int Critical)> GetActiveCountAsync(CancellationToken ct = default);
    Task<bool> ExistsUsingUnidadMedidaAsync(Guid unidadMedidaId, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveWithCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
}
