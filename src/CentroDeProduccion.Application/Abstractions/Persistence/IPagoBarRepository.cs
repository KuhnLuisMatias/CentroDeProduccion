using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IPagoBarRepository
{
    Task<PagoBar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagoBar?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PagoBar>> GetByFiltersAsync(
        Guid? barId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPaidForRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default);
    Task AddAsync(PagoBar pagoBar, CancellationToken cancellationToken = default);
}
