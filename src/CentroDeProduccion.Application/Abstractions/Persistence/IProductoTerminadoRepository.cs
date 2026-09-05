using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IProductoTerminadoRepository
{
    Task<ProductoTerminado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoTerminado>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<int> GetStockTotalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoTerminado>> GetProximosAVencerAsync(DateTime hasta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductoTerminado>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Batch load that keeps entities TRACKED by the DbContext. Any handler that
    /// mutates loaded entities (e.g. StockActual) before SaveChanges must use this method:
    /// <see cref="GetByIdsAsync"/> is AsNoTracking, so mutations on its results are silently
    /// discarded.</summary>
    Task<IReadOnlyList<ProductoTerminado>> GetTrackedByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default);

    /// <summary>Tracked find by exact name, case-insensitive (used by producción simple's
    /// find-or-create of the finished product derived from the recipe).</summary>
    Task<ProductoTerminado?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);

    /// <summary>TRACKED active finished product derived from <paramref name="recetaId"/> (used by
    /// producción simple to deduct sub-recipe consumption from that product's stock at confirm).</summary>
    Task<ProductoTerminado?> GetTrackedActiveByRecetaIdAsync(Guid recetaId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductoTerminado producto, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveWithCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
}
