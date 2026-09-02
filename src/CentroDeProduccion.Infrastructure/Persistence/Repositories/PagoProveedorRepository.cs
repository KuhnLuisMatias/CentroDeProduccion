using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class PagoProveedorRepository : IPagoProveedorRepository
{
    private readonly AppDbContext _context;

    public PagoProveedorRepository(AppDbContext context) => _context = context;

    public async Task<PagoProveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.PagosProveedor.FindAsync([id], cancellationToken);

    public async Task<PagoProveedor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.PagosProveedor
            .Include(pp => pp.Proveedor)
            .Include(pp => pp.Metodos)
            .Include(pp => pp.Insumos)
            .ThenInclude(pi => pi.Insumo)
            .FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PagoProveedor>> GetByFiltersAsync(
        Guid? proveedorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.PagosProveedor
            .Include(pp => pp.Proveedor)
            .Include(pp => pp.Metodos)
            .Include(pp => pp.Insumos)
            .ThenInclude(pi => pi.Insumo)
            .Where(pp =>
                (!proveedorId.HasValue || pp.ProveedorId == proveedorId.Value) &&
                (!fechaDesde.HasValue || pp.FechaPago >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || pp.FechaPago < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(pp => pp.FechaPago)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default)
        => (await _context.PagosProveedor.MaxAsync(pp => (int?)pp.Numero, cancellationToken) ?? 0) + 1;

    public async Task AddAsync(PagoProveedor pago, CancellationToken cancellationToken = default)
        => await _context.PagosProveedor.AddAsync(pago, cancellationToken);
}