using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class RecetaRepository : IRecetaRepository
{
    private readonly AppDbContext _context;

    public RecetaRepository(AppDbContext context) => _context = context;

    public async Task<Receta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Recetas.FindAsync([id], cancellationToken);

    public async Task<Receta?> GetByIdWithDetallesAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Recetas
            .Include(r => r.Categoria)
            .Include(r => r.UnidadMedida)
            .Include(r => r.Insumos).ThenInclude(ri => ri.Insumo)
            .Include(r => r.Insumos).ThenInclude(ri => ri.RecetaOrigen)
            .Include(r => r.Insumos).ThenInclude(ri => ri.UnidadMedida)
            .Include(r => r.Presentaciones).ThenInclude(pv => pv.UnidadMedida)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Receta>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Recetas
            .Where(r => r.Activo)
            .Include(r => r.Categoria)
            .Include(r => r.UnidadMedida)
            .OrderBy(r => r.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithSkuAsync(string codigoSku, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.Recetas.AnyAsync(r =>
            r.CodigoSku == codigoSku &&
            (!excludingId.HasValue || r.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(Receta receta, CancellationToken cancellationToken = default)
        => await _context.Recetas.AddAsync(receta, cancellationToken);

    public async Task<IReadOnlyList<RecetaVersion>> GetVersionesAsync(Guid recetaId, CancellationToken cancellationToken = default)
        => await _context.RecetaVersiones
            .Where(rv => rv.RecetaId == recetaId)
            .OrderByDescending(rv => rv.Version)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
