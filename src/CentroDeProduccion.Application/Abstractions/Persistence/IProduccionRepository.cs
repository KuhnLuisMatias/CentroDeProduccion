using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IProduccionRepository
{
    Task<Produccion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Produccion?> GetByIdWithSalidasAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Produccion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Produccion produccion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Produccion>> GetByFiltersAsync(
        DateTime from,
        DateTime to,
        Guid? recetaId = null,
        EstadoProduccion? estado = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Produccion>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Produccion>> GetByFiltersWithSalidasAsync(
        DateTime from,
        DateTime to,
        Guid? recetaId = null,
        EstadoProduccion? estado = null,
        CancellationToken ct = default);
}
