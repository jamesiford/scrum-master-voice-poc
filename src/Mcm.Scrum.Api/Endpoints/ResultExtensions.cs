using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Api.Endpoints;

/// <summary>Maps <see cref="Result"/> to HTTP in one place so endpoints never hand-write status codes.</summary>
public static class ResultExtensions
{
    /// <summary>Maps a result to a 204 (or 200 with a value) or the PoC error body <c>{ "error", "detail" }</c>.</summary>
    /// <param name="result">The result.</param>
    /// <returns>An HTTP result.</returns>
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Error is { } error ? ToProblem(error) : Results.NoContent();
    }

    /// <summary>Maps a result with a value to 200 with the value, or the PoC error body.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>An HTTP result.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Error is { } error ? ToProblem(error) : Results.Ok(result.Value);
    }

    private static IResult ToProblem(Error error) =>
        Results.Json(new { error = error.Code, detail = error.Message }, statusCode: StatusFor(error.Type));

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.NotApproved => StatusCodes.Status403Forbidden,
        ErrorType.ExternalRejected => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
