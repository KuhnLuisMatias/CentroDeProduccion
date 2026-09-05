using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class InsumoRepository : IInsumoRepository
{
    private readonly AppDbContext _context;

    public InsumoRepository(AppDbContext context) => _context = context;

    public async Task<Insumo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Insumos
            .Include(i => i.Categoria)
            .Include(i => i.UnidadCompra)
            .Include(i => i.UnidadConsumo)
            .Include(i => i.ProveedorPrincipal)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.Insumos.AnyAsync(i =>
            i.CodigoSku == codigoSku &&
            (!excludingId.HasValue || i.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(Insumo insumo, CancellationToken cancellationToken = default)
        => await _context.Insumos.AddAsync(insumo, cancellationToken);

    public async Task<IReadOnlyList<Insumo>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Insumos
            .Where(i => i.Activo)
            .Include(i => i.Categoria)
            .Include(i => i.UnidadCompra)
            .Include(i => i.UnidadConsumo)
            .Include(i => i.ProveedorPrincipal)
            .OrderBy(i => i.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<int> GetCriticosCountAsync(CancellationToken cancellationToken = default)
        => await _context.Insumos.CountAsync(i => i.Activo && i.StockActual <= i.StockMinimo, cancellationToken);

    public async Task<Insumo?> GetByIdWithMovementsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Insumos
            .Include(i => i.Movimientos.OrderByDescending(m => m.Fecha).Take(50))
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<PagedResult<Insumo>> GetPagedAsync(string? searchTerm, int page, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Insumo> query = _context.Insumos
            .Include(i => i.Categoria)
            .Include(i => i.UnidadCompra)
            .Include(i => i.UnidadConsumo)
            .Include(i => i.ProveedorPrincipal);

        if (!includeInactive)
        {
            query = query.Where(i => i.Activo);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var isPresentacion = decimal.TryParse(term, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var presentacionTerm) ||
                decimal.TryParse(term, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture, out presentacionTerm);
            query = query.Where(i =>
                i.Nombre.Contains(term) ||
                i.CodigoSku.Contains(term) ||
                i.Categoria!.Nombre.Contains(term) ||
                (isPresentacion && i.Presentacion == presentacionTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(i => i.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Insumo>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<Insumo>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return Array.Empty<Insumo>();

        return await _context.Insumos
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Total, int Critical)> GetActiveCountAsync(CancellationToken ct = default)
    {
        var total = await _context.Insumos.CountAsync(i => i.Activo, ct);
        var critical = await _context.Insumos.CountAsync(i => i.Activo && i.StockActual <= i.StockMinimo, ct);
        return (total, critical);
    }

    public async Task<bool> ExistsUsingUnidadMedidaAsync(Guid unidadMedidaId, CancellationToken cancellationToken = default)
        => await _context.Insumos.AnyAsync(i =>
            i.Activo && (i.UnidadCompraId == unidadMedidaId || i.UnidadConsumoId == unidadMedidaId),
            cancellationToken);

    public async Task<bool> ExistsActiveWithCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        => await _context.Insumos.AnyAsync(i => i.CategoriaId == categoriaId && i.Activo, cancellationToken);
}
