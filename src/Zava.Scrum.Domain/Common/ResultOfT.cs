namespace Zava.Scrum.Domain.Common;

/// <summary>Outcome of an operation that returns a value.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Result<T> : Result, IResultFactory<Result<T>>
{
    private readonly T? _value;

    private Result(T? value, Error? error)
        : base(error) => _value = value;

    /// <summary>Gets the value. Throws if the result is a failure.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    /// <summary>Creates a successful result.</summary>
    /// <param name="value">The value.</param>
    /// <returns>A success.</returns>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>A failure.</returns>
    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, error);
    }
}
