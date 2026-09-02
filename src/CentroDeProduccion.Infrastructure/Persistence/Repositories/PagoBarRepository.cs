using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class PagoBarRepository : IPagoBarRepository
{
    private readonly AppDbContext _context;

    public PagoBarRepository(AppDbContext context) => _context = context;

    public async Task<PagoBar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<PagoBar>().FindAsync([id], cancellationToken);

    public async Task<PagoBar?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<PagoBar>()
            .Include(pb => pb.Bar)
            .Include(pb => pb.Metodos)
            .Include(pb => pb.Items)
                .ThenInclude(i => i.Remito)
            .FirstOrDefaultAsync(pb => pb.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PagoBar>> GetByFiltersAsync(
        Guid? barId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.Set<PagoBar>()
            .Include(pb => pb.Bar)
            .Include(pb => pb.Metodos)
            .Include(pb => pb.Items)
            .Where(pb =>
                (!barId.HasValue || pb.BarId == barId.Value) &&
                (!fechaDesde.HasValue || pb.FechaPago >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || pb.FechaPago < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(pb => pb.FechaPago)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default)
        => (await _context.Set<PagoBar>().MaxAsync(pb => (int?)pb.Numero, cancellationToken) ?? 0) + 1;

    public async Task<decimal> GetTotalPaidForRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default)
        => await _context.Set<PagoBarItem>()
            .Where(i => i.RemitoId == remitoId)
            .SumAsync(i => (decimal?)i.MontoAplicado, cancellationToken) ?? 0m;

    public async Task AddAsync(PagoBar pagoBar, CancellationToken cancellationToken = default)
        => await _context.Set<PagoBar>().AddAsync(pagoBar, cancellationToken);
}
