using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class BarRepository : IBarRepository
{
    private readonly AppDbContext _context;

    public BarRepository(AppDbContext context) => _context = context;

    public async Task<Bar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<Bar>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Bar>> GetByFiltersAsync(
        EstadoBar? estado,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Bar> query = _context.Set<Bar>();

        if (estado.HasValue)
            query = query.Where(b => b.Estado == estado.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(b => b.Nombre.Contains(term));
        }

        return await query
            .OrderBy(b => b.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNombreAsync(string nombre, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _context.Set<Bar>().AnyAsync(b =>
            b.Nombre == nombre &&
            (!excludeId.HasValue || b.Id != excludeId.Value),
            cancellationToken);

    public async Task AddAsync(Bar bar, CancellationToken cancellationToken = default)
        => await _context.Set<Bar>().AddAsync(bar, cancellationToken);
}
