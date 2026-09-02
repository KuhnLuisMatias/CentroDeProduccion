using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IUnidadMedidaRepository
{
    Task<UnidadMedida?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnidadMedida?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnidadMedida>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNombreAsync(string nombre, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithSimboloAsync(string simbolo, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default);
}
