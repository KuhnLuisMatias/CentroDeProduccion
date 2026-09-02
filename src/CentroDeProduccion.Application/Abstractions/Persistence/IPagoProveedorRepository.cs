using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IPagoProveedorRepository
{
    Task<PagoProveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagoProveedor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PagoProveedor>> GetByFiltersAsync(
        Guid? proveedorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default);

    Task AddAsync(PagoProveedor pago, CancellationToken cancellationToken = default);
}