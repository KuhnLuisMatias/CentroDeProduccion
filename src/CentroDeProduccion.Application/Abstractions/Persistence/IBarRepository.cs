using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IBarRepository
{
    Task<Bar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bar>> GetByFiltersAsync(
        EstadoBar? estado,
        string? searchTerm,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Bar bar, CancellationToken cancellationToken = default);
}
