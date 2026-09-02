using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentroDeProduccion.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO(diag): remove after concurrency investigation
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Unchanged ||
                entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            {
                continue;
            }
            var idProp = entry.Entity.GetType().GetProperty("Id");
            var idVal = idProp?.GetValue(entry.Entity);
            _logger.LogWarning(
                "CONCURRENCY-DIAG pre-save entry={EntityType} id={Id} state={State}",
                entry.Entity.GetType().Name,
                idVal,
                entry.State);
        }

        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // TODO(diag): remove after concurrency investigation
            foreach (var entry in ex.Entries)
            {
                _logger.LogError(
                    "CONCURRENCY-DIAG entry={EntityType} state={State}",
                    entry.Entity.GetType().Name,
                    entry.State);
            }
            throw new ConcurrencyConflictException(
                "Se detectó un conflicto de concurrencia optimista al guardar los cambios.", ex);
        }
    }
}
