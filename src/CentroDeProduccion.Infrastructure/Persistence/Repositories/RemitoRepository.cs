using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class RemitoRepository : IRemitoRepository
{
    private readonly AppDbContext _context;

    public RemitoRepository(AppDbContext context) => _context = context;

    public async Task<Remito?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Remito>().FindAsync([id], cancellationToken);

    public async Task<Remito?> GetByIdWithLineasAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Remito>()
            .Include(r => r.Bar)
            .Include(r => r.Lineas)
                .ThenInclude(l => l.ProductoTerminado)
            .Include(r => r.Lineas)
                .ThenInclude(l => l.Insumo)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Remito>> GetByFiltersAsync(
        Guid? barId,
        EstadoRemito? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.Set<Remito>()
            .Include(r => r.Bar)
            .Include(r => r.Lineas)
            .Where(r =>
                (!barId.HasValue || r.BarId == barId.Value) &&
                (!estado.HasValue || r.Estado == estado.Value) &&
                (!fechaDesde.HasValue || r.Fecha >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || r.Fecha < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(r => r.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default)
        => (await _context.Set<Remito>().MaxAsync(r => (int?)r.NumeroRemito, cancellationToken) ?? 0) + 1;

    public async Task AddAsync(Remito remito, CancellationToken cancellationToken = default)
        => await _context.Set<Remito>().AddAsync(remito, cancellationToken);
}
