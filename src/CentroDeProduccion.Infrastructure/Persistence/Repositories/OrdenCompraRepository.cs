using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class OrdenCompraRepository : IOrdenCompraRepository
{
    private readonly AppDbContext _context;

    public OrdenCompraRepository(AppDbContext context) => _context = context;

    public async Task<OrdenCompra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.OrdenesCompra.FindAsync([id], cancellationToken);

    public async Task<OrdenCompra?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.OrdenesCompra
            .Include(oc => oc.Items)
            .ThenInclude(i => i.Insumo)
            .Include(oc => oc.Proveedor)
            .FirstOrDefaultAsync(oc => oc.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OrdenCompra>> GetByFiltersAsync(
        Guid? proveedorId,
        EstadoOrdenCompra? estado,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.OrdenesCompra
            .Include(oc => oc.Items)
            .ThenInclude(i => i.Insumo)
            .Include(oc => oc.Proveedor)
            .Where(oc =>
                (!proveedorId.HasValue || oc.ProveedorId == proveedorId.Value) &&
                (!estado.HasValue || oc.Estado == estado.Value) &&
                (!fechaDesde.HasValue || oc.FechaCreacion >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || oc.FechaCreacion < fechaHasta.Value.Date.AddDays(1)))
            .OrderByDescending(oc => oc.FechaCreacion)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextNumeroAsync(CancellationToken cancellationToken = default)
        => (await _context.OrdenesCompra.MaxAsync(oc => (int?)oc.Numero, cancellationToken) ?? 0) + 1;

    public async Task AddAsync(OrdenCompra ordenCompra, CancellationToken cancellationToken = default)
        => await _context.OrdenesCompra.AddAsync(ordenCompra, cancellationToken);
}