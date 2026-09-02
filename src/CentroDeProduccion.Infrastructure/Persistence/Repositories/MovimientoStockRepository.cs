using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly AppDbContext _context;

    public MovimientoStockRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(MovimientoStock movimiento, CancellationToken cancellationToken = default)
        => await _context.MovimientosStock.AddAsync(movimiento, cancellationToken);

    public async Task<IReadOnlyList<MovimientoStock>> GetByInsumoIdAsync(Guid insumoId, CancellationToken cancellationToken = default)
        => await _context.MovimientosStock
            .Where(m => m.InsumoId == insumoId)
            .Include(m => m.Usuario)
            .Include(m => m.UnidadOriginal)
            .OrderByDescending(m => m.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MovimientoStock>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await _context.MovimientosStock
            .Where(m => m.Fecha >= from && m.Fecha < to.Date.AddDays(1))
            .Include(m => m.Insumo)
            .Include(m => m.Usuario)
            .OrderByDescending(m => m.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MovimientoStock>> GetByFiltersAsync(
        DateTime from,
        DateTime to,
        Guid? insumoId = null,
        Guid? productoTerminadoId = null,
        TipoMovimientoStock? tipo = null,
        CancellationToken ct = default)
        => await _context.MovimientosStock
            .Where(m =>
                m.Fecha >= from &&
                m.Fecha < to.Date.AddDays(1) &&
                (!insumoId.HasValue || m.InsumoId == insumoId.Value) &&
                (!productoTerminadoId.HasValue || m.ProductoTerminadoId == productoTerminadoId.Value) &&
                (!tipo.HasValue || m.Tipo == tipo.Value))
            .Include(m => m.Insumo)
            .Include(m => m.Usuario)
            .OrderByDescending(m => m.Fecha)
            .AsNoTracking()
            .ToListAsync(ct);
}
