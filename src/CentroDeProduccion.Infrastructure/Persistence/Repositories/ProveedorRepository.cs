using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly AppDbContext _context;

    public ProveedorRepository(AppDbContext context) => _context = context;

    public async Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Proveedores.FindAsync([id], cancellationToken);

    public async Task<bool> ExistsWithCuitAsync(string cuit, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.Proveedores.AnyAsync(p =>
            p.Cuit == cuit &&
            (!excludingId.HasValue || p.Id != excludingId.Value),
            cancellationToken);

    public async Task<IReadOnlyList<Proveedor>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.NombreRazonSocial)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Proveedor>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Proveedor>();

        return await _context.Proveedores
            .Where(p => ids.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Proveedor proveedor, CancellationToken cancellationToken = default)
        => await _context.Proveedores.AddAsync(proveedor, cancellationToken);
}
