using CentroDeProduccion.Domain.Entities;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IPagoAProveedorRepository
{
    Task AddAsync(PagoAProveedor pago, CancellationToken cancellationToken = default);

    Task<PagoAProveedor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PagoAProveedor>> GetByFiltersAsync(
        Guid? proveedorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
}
