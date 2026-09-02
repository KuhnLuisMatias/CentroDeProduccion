using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class InventarioSesionRepository : IInventarioSesionRepository
{
    private readonly AppDbContext _context;

    public InventarioSesionRepository(AppDbContext context) => _context = context;

    public async Task<Guid> AddAsync(InventarioSesion session, CancellationToken ct = default)
    {
        await _context.Set<InventarioSesion>().AddAsync(session, ct);
        return session.Id;
    }

    public async Task<InventarioSesion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<InventarioSesion>().FindAsync([id], ct);

    public async Task<InventarioSesion?> GetByIdWithConteosAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<InventarioSesion>()
            .Include(i => i.Conteos)
                .ThenInclude(c => c.Insumo)
            .Include(i => i.Conteos)
                .ThenInclude(c => c.ProductoTerminado)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<InventarioSesion>> GetAllAsync(CancellationToken ct = default)
        => await _context.Set<InventarioSesion>()
            .OrderByDescending(i => i.Fecha)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddConteoAsync(InventarioConteo conteo, CancellationToken ct = default)
        => await _context.Set<InventarioConteo>().AddAsync(conteo, ct);
}
