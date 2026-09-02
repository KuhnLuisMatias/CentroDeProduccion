using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class UnidadMedidaRepository : IUnidadMedidaRepository
{
    private readonly AppDbContext _context;

    public UnidadMedidaRepository(AppDbContext context) => _context = context;

    public async Task<UnidadMedida?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida.FindAsync([id], cancellationToken);

    public async Task<UnidadMedida?> GetByNombreAsync(string nombre, CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida.FirstOrDefaultAsync(u => u.Nombre == nombre, cancellationToken);

    public async Task<IReadOnlyList<UnidadMedida>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithNombreAsync(string nombre, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida.AnyAsync(u =>
            u.Nombre == nombre &&
            (!excludingId.HasValue || u.Id != excludingId.Value),
            cancellationToken);

    public async Task<bool> ExistsWithSimboloAsync(string simbolo, Guid? excludingId = null, CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida.AnyAsync(u =>
            u.Simbolo == simbolo &&
            (!excludingId.HasValue || u.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(UnidadMedida unidadMedida, CancellationToken cancellationToken = default)
        => await _context.UnidadesMedida.AddAsync(unidadMedida, cancellationToken);
}
