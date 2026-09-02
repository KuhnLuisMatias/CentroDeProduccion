namespace CentroDeProduccion.Application.Abstractions.Time;

/// <summary>
/// Testable wall-clock seam. Handlers depend on this instead of <see cref="DateTime.UtcNow"/>
/// directly so tests can freeze time (token expiry, rotation timestamps, seed guards).
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
