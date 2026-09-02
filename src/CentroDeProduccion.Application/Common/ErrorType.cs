namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Classifies an <see cref="Error"/> so <c>Api/Extensions/ResultExtensions</c> can map it to
/// exactly one HTTP status / RFC 7807 ProblemDetails shape. This is the only place in the
/// solution that decides "what kind of failure is this" — handlers never throw for expected
/// failures, they return a <see cref="Result"/> carrying one of these.
/// </summary>
public enum ErrorType
{
    /// <summary>No error. Used internally by <see cref="Error.None"/>.</summary>
    None = 0,

    /// <summary>Input failed validation (FluentValidation or handler-level checks). Maps to 400.</summary>
    Validation,

    /// <summary>Missing or invalid credentials. Maps to 401.</summary>
    Unauthorized,

    /// <summary>Authenticated but not allowed to perform this action. Maps to 403.</summary>
    Forbidden,

    /// <summary>Requested resource does not exist. Maps to 404.</summary>
    NotFound,

    /// <summary>Request conflicts with existing state (duplicate key, business rule). Maps to 409.</summary>
    Conflict,

    /// <summary>Optimistic concurrency token mismatch after retries exhausted. Maps to 409.</summary>
    Concurrency,

    /// <summary>Unclassified failure. Maps to 500 with no detail in the response body.</summary>
    Unexpected
}
