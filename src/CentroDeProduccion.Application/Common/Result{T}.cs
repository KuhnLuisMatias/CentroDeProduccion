namespace CentroDeProduccion.Application.Common;

/// <summary>
/// Outcome of an operation that returns <typeparamref name="TValue"/> on success. See
/// <see cref="Result"/> for the rationale (no control-flow exceptions for expected failures).
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value) : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> if accessed on a
    /// failed result — callers must check <see cref="Result.IsSuccess"/> first.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<TValue> Success(TValue value) => new(value);
    public static new Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
