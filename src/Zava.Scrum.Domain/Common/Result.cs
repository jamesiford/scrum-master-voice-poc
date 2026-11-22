namespace Zava.Scrum.Domain.Common;

/// <summary>Outcome of an operation without a value. Expected failures use this, not exceptions.</summary>
public class Result : IResultFactory<Result>
{
    /// <summary>Initializes a new instance of the <see cref="Result"/> class.</summary>
    /// <param name="error">The failure, or <see langword="null"/> on success.</param>
    protected Result(Error? error) => Error = error;

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Gets the failure, or <see langword="null"/> on success.</summary>
    public Error? Error { get; }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A success.</returns>
    public static Result Success() => new(null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>A failure.</returns>
    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }
}
