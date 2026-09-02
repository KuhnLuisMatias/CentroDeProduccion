using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IRemitoRepository
{
    Task<Remito?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Remito?> GetByIdWithLineasAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Remito>> GetByFiltersAsync(
        Guid? barId,
        EstadoRemito? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Remito remito, CancellationToken cancellationToken = default);
}
