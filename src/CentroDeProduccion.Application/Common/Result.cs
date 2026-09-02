namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Non-generic outcome of an operation that returns no value on success. Application handlers
/// return this (or <see cref="Result{TValue}"/>) instead of throwing for expected failures;
/// <c>Api/Extensions/ResultExtensions.ToActionResult()</c> is the single place that turns a
/// failed <see cref="Result"/> into an RFC 7807 ProblemDetails response.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}
