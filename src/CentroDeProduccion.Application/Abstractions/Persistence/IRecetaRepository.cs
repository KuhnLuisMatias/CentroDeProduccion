using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IRecetaRepository
{
    Task<Receta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Receta?> GetByIdWithDetallesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Receta>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Receta receta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecetaVersion>> GetVersionesAsync(Guid recetaId, CancellationToken cancellationToken = default);
}
