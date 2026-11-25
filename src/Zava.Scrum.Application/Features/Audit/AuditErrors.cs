using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Audit;

/// <summary>Error codes for the audit chain.</summary>
public static class AuditErrors
{
    /// <summary>Gets the error for a reference that is not a trace id or a <c>zsa-</c> tag.</summary>
    public static Error InvalidReference { get; } = Error.Invalid("audit.invalid_reference", "Give the 32-character correlation id, or the zsa- tag from an Azure DevOps comment (at least 12 hex characters).");

    /// <summary>Gets the error for a correlation id with no session, follow-up or record.</summary>
    /// <param name="reference">What was asked for.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string reference) => Error.NotFound("audit.not_found", $"Nothing is recorded for correlation '{reference}'.");
}
