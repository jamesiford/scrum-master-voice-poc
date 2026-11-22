namespace Zava.Scrum.Domain.Common;

/// <summary>An expected failure carried by a <see cref="Result"/>.</summary>
/// <param name="Code">Stable machine-readable code, for example <c>health.store_unavailable</c>.</param>
/// <param name="Message">Human-readable detail safe to show in the portal.</param>
/// <param name="Type">Category used for transport mapping.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Invalid(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates a <see cref="ErrorType.NotApproved"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error NotApproved(string code, string message) => new(code, message, ErrorType.NotApproved);

    /// <summary>Creates a <see cref="ErrorType.ExternalRejected"/> error.</summary>
    /// <param name="code">Error code.</param>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error ExternalRejected(string code, string message) => new(code, message, ErrorType.ExternalRejected);

    /// <summary>Creates a <see cref="ErrorType.Unexpected"/> error.</summary>
    /// <param name="message">Error message.</param>
    /// <returns>The error.</returns>
    public static Error Unexpected(string message) => new("unexpected", message, ErrorType.Unexpected);
}
