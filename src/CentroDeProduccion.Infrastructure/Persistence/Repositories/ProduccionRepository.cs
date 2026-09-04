using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class ProduccionRepository : IProduccionRepository
{
    private readonly AppDbContext _context;

    public ProduccionRepository(AppDbContext context) => _context = context;

    public async Task<Produccion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Producciones.FindAsync([id], cancellationToken);

    public async Task<Produccion?> GetByIdWithSalidasAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Producciones
            .Include(p => p.Receta).ThenInclude(r => r.UnidadMedida)
            .Include(p => p.Responsable)
            .Include(p => p.Salidas).ThenInclude(ps => ps.ProductoTerminado)
            .Include(p => p.InsumosConsumidos).ThenInclude(pi => pi.Insumo)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Produccion>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Producciones
            .Include(p => p.Receta)
            .Include(p => p.Responsable)
            .Include(p => p.InsumosConsumidos).ThenInclude(pi => pi.Insumo)
            .OrderByDescending(p => p.Fecha)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Produccion produccion, CancellationToken cancellationToken = default)
        => await _context.Producciones.AddAsync(produccion, cancellationToken);

    public async Task<IReadOnlyList<Produccion>> GetByFiltersAsync(
        DateTime from,
        DateTime to,
        Guid? recetaId = null,
        EstadoProduccion? estado = null,
        CancellationToken ct = default)
        => await _context.Producciones
            .Where(p =>
                p.Fecha >= from &&
                p.Fecha < to.Date.AddDays(1) &&
                (!recetaId.HasValue || p.RecetaId == recetaId.Value) &&
                (!estado.HasValue || p.Estado == estado.Value))
            .Include(p => p.Receta)
            .Include(p => p.Responsable)
            .OrderByDescending(p => p.Fecha)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Produccion>> GetByFiltersWithSalidasAsync(
        DateTime from,
        DateTime to,
        Guid? recetaId = null,
        EstadoProduccion? estado = null,
        CancellationToken ct = default)
        => await _context.Producciones
            .Where(p =>
                p.Fecha >= from &&
                p.Fecha < to.Date.AddDays(1) &&
                (!recetaId.HasValue || p.RecetaId == recetaId.Value) &&
                (!estado.HasValue || p.Estado == estado.Value))
            .Include(p => p.Receta)
            .Include(p => p.Responsable)
            .Include(p => p.Salidas).ThenInclude(s => s.ProductoTerminado)
            .OrderByDescending(p => p.Fecha)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Produccion>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.Producciones
            .Where(p => p.Fecha >= from && p.Fecha < to.Date.AddDays(1))
            .Include(p => p.Receta)
            .Include(p => p.Responsable)
            .OrderBy(p => p.Fecha)
            .AsNoTracking()
            .ToListAsync(ct);
}
