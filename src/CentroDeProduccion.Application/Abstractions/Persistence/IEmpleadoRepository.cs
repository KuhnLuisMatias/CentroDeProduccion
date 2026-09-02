using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Abstractions.Persistence;

public interface IEmpleadoRepository
{
    Task<Empleado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Empleado>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Empleado>> GetAllAsync(
        bool? activo,
        CargoEmpleado? cargo,
        CategoriaEmpleado? categoria,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsWithDniAsync(string dni, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Empleado empleado, CancellationToken cancellationToken = default);
    Task DeleteAsync(Empleado empleado, CancellationToken cancellationToken = default);
}