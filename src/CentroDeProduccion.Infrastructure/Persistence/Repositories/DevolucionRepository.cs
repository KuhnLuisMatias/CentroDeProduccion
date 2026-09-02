using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class DevolucionRepository : IDevolucionRepository
{
    private readonly AppDbContext _context;

    public DevolucionRepository(AppDbContext context) => _context = context;

    public async Task<Devolucion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>().FindAsync([id], cancellationToken);

    public async Task<Devolucion?> GetByIdWithLineasAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>()
            .Include(d => d.Lineas)
                .ThenInclude(l => l.ProductoTerminado)
            .Include(d => d.Remito)
                .ThenInclude(r => r.Bar)
            .Include(d => d.Remito)
                .ThenInclude(r => r.Lineas)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Devolucion>> GetByFiltersAsync(
        Guid? remitoId,
        Guid? barId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>()
            .Include(d => d.Lineas)
            .Include(d => d.Remito)
                .ThenInclude(r => r.Bar)
            .Include(d => d.Remito)
                .ThenInclude(r => r.Lineas)
            .Where(d =>
                (!remitoId.HasValue || d.RemitoId == remitoId.Value) &&
                (!barId.HasValue || d.Remito.BarId == barId.Value) &&
                (!fechaDesde.HasValue || d.Fecha >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || d.Fecha < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(d => d.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default)
        => (await _context.Set<Devolucion>().MaxAsync(d => (int?)d.Numero, cancellationToken) ?? 0) + 1;

    public async Task<decimal> GetTotalDevueltoForLineAsync(Guid remitoId, Guid productoTerminadoId, CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>()
            .Where(d => d.RemitoId == remitoId)
            .SelectMany(d => d.Lineas)
            .Where(l => l.ProductoTerminadoId == productoTerminadoId)
            .SumAsync(l => (decimal?)l.Cantidad, cancellationToken) ?? 0m;

    public async Task<Dictionary<Guid, decimal>> GetTotalesDevueltosPorRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>()
            .Where(d => d.RemitoId == remitoId)
            .SelectMany(d => d.Lineas)
            .GroupBy(l => l.ProductoTerminadoId)
            .Select(g => new { ProductoTerminadoId = g.Key, Total = g.Sum(l => l.Cantidad) })
            .ToDictionaryAsync(x => x.ProductoTerminadoId, x => x.Total, cancellationToken);

    public async Task AddAsync(Devolucion devolucion, CancellationToken cancellationToken = default)
        => await _context.Set<Devolucion>().AddAsync(devolucion, cancellationToken);
}
