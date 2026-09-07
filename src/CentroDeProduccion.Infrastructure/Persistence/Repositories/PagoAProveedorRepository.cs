using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class PagoAProveedorRepository : IPagoAProveedorRepository
{
    private readonly AppDbContext _context;

    public PagoAProveedorRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(PagoAProveedor pago, CancellationToken cancellationToken = default)
        => await _context.PagosAProveedores.AddAsync(pago, cancellationToken);

    public async Task<PagoAProveedor?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.PagosAProveedores
            .Include(pa => pa.Proveedor)
            .Include(pa => pa.Medios)
            .FirstOrDefaultAsync(pa => pa.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PagoAProveedor>> GetByFiltersAsync(
        Guid? proveedorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.PagosAProveedores
            .Include(pa => pa.Proveedor)
            .Include(pa => pa.Medios)
            .Where(pa =>
                (!proveedorId.HasValue || pa.ProveedorId == proveedorId.Value) &&
                (!fechaDesde.HasValue || pa.Fecha >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || pa.Fecha < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(pa => pa.Fecha)
            .ThenByDescending(pa => pa.FechaCreacion)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
