namespace CentroDeProduccion.Application.Common;

/// <summary>
/// A single expected failure: a stable machine-readable <paramref name="Code"/>, a
/// human-readable <paramref name="Message"/>, and an <see cref="ErrorType"/> that decides the
/// HTTP mapping. Handlers construct these instead of throwing for expected failures.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Sentinel representing "no error", paired with a successful <see cref="Result"/>.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Concurrency(string code, string message) => new(code, message, ErrorType.Concurrency);
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
