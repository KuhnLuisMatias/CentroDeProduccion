using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly AppDbContext _context;

    public EmpleadoRepository(AppDbContext context) => _context = context;

    public async Task<Empleado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Empleados.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Empleado>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Empleados
            .Where(e => e.Activo)
            .OrderBy(e => e.Apellido)
            .ThenBy(e => e.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Empleado>> GetAllAsync(
        bool? activo,
        CargoEmpleado? cargo,
        CategoriaEmpleado? categoria,
        CancellationToken cancellationToken = default)
        => await _context.Empleados
            .Where(e =>
                (!activo.HasValue || e.Activo == activo.Value) &&
                (!cargo.HasValue || e.Cargo == cargo.Value) &&
                (!categoria.HasValue || e.Categoria == categoria.Value))
            .OrderBy(e => e.Apellido)
            .ThenBy(e => e.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithDniAsync(string dni, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.Empleados.AnyAsync(e =>
            e.Dni == dni &&
            (!excludingId.HasValue || e.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(Empleado empleado, CancellationToken cancellationToken = default)
        => await _context.Empleados.AddAsync(empleado, cancellationToken);

    public Task DeleteAsync(Empleado empleado, CancellationToken cancellationToken = default)
    {
        _context.Empleados.Remove(empleado);
        return Task.CompletedTask;
    }
}