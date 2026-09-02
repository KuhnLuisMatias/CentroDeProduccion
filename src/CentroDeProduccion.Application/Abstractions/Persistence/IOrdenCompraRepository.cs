using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IOrdenCompraRepository
{
    Task<OrdenCompra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrdenCompra?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrdenCompra>> GetByFiltersAsync(
        Guid? proveedorId,
        EstadoOrdenCompra? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default);
    Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default);
    Task AddAsync(OrdenCompra ordenCompra, CancellationToken cancellationToken = default);
}