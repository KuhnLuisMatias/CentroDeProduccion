using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly AppDbContext _context;

    public CategoriaRepository(AppDbContext context) => _context = context;

    public async Task<Categoria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Categorias.FindAsync([id], cancellationToken);

    public async Task<bool> ExistsWithNameInAmbitoAsync(string nombre, AmbitoCategoria ambito, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.Categorias.AnyAsync(c =>
            c.Nombre == nombre &&
            c.Ambito == ambito &&
            (!excludingId.HasValue || c.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(Categoria categoria, CancellationToken cancellationToken = default)
        => await _context.Categorias.AddAsync(categoria, cancellationToken);

    public async Task<IReadOnlyList<Categoria>> GetAllByAmbitoAsync(AmbitoCategoria ambito, CancellationToken cancellationToken = default)
        => await _context.Categorias
            .Where(c => c.Ambito == ambito && c.Activo)
            .OrderBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
