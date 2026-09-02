namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Raised by <see cref="CentroDeProduccion.Application.Abstractions.Persistence.IUnitOfWork"/>
/// implementations when the underlying store reports an optimistic-concurrency conflict. The
/// Application layer maps this to <see cref="Error.Concurrency"/> without referencing any
/// persistence framework.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
