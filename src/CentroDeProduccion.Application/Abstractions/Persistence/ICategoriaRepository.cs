using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface ICategoriaRepository
{
    Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameInAmbitoAsync(string nombre, AmbitoCategoria ambito, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Categoria categoria, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Categoria>> GetAllByAmbitoAsync(AmbitoCategoria ambito, CancellationToken cancellationToken = default);
}
