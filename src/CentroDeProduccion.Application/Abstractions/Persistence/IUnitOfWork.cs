namespace CentroDeProduccion.Application.Abstractions.Persistence;

/// <summary>
/// Commits the changes tracked by the current unit of work in a single transaction. Repository
/// methods mutate tracked entities; handlers call <see cref="SaveChangesAsync"/> once per use case.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
