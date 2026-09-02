using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context) => _context = context;

    public async Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Usuarios.FindAsync([id], cancellationToken);

    public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await _context.Usuarios.AnyAsync(cancellationToken);

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => await _context.Usuarios.AddAsync(usuario, cancellationToken);
}
