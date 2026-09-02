using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class ProductoTerminadoRepository : IProductoTerminadoRepository
{
    private readonly AppDbContext _context;

    public ProductoTerminadoRepository(AppDbContext context) => _context = context;

    public async Task<ProductoTerminado?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados
            .Include(pt => pt.Categoria)
            .Include(pt => pt.UnidadMedida)
            .Include(pt => pt.Receta)
            .FirstOrDefaultAsync(pt => pt.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProductoTerminado>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados
            .Where(pt => pt.Activo)
            .Include(pt => pt.Categoria)
            .Include(pt => pt.UnidadMedida)
            .Include(pt => pt.Receta)
            .OrderBy(pt => pt.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetStockTotalAsync(CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados.CountAsync(pt => pt.Activo, cancellationToken);

    public async Task<IReadOnlyList<ProductoTerminado>> GetProximosAVencerAsync(DateTime hasta, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados
            .Where(pt => pt.Activo && pt.FechaVencimiento < hasta.Date.AddDays(1) && pt.Estado != Domain.Enums.EstadoProductoTerminado.Vencido)
            .Include(pt => pt.Categoria)
            .OrderBy(pt => pt.FechaVencimiento)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductoTerminado>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<ProductoTerminado>();

        return await _context.ProductosTerminados
            .Where(pt => ids.Contains(pt.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoTerminado>> GetTrackedByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<ProductoTerminado>();

        // Deliberately NO AsNoTracking: callers mutate these entities and rely on the
        // change tracker to persist them at SaveChanges.
        return await _context.ProductosTerminados
            .Where(pt => ids.Contains(pt.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados.AnyAsync(pt =>
            pt.CodigoSku == codigoSku &&
            (!excludingId.HasValue || pt.Id != excludingId.Value),
            cancellationToken);

    public async Task<ProductoTerminado?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados
            .Include(pt => pt.UnidadMedida)
            .FirstOrDefaultAsync(pt => pt.Nombre.ToLower() == nombre.ToLower(), cancellationToken);

    public async Task AddAsync(ProductoTerminado producto, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados.AddAsync(producto, cancellationToken);

    public async Task<bool> ExistsActiveWithCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        => await _context.ProductosTerminados.AnyAsync(pt => pt.CategoriaId == categoriaId && pt.Activo, cancellationToken);
}
