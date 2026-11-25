using System.Diagnostics;

namespace Zava.Scrum.Domain.Common;

/// <summary>
/// The correlation id of a session: a W3C trace id (32 lower-case hex characters). It is created when the session is created and
/// carried through the meeting join, each turn, the proposal, the approval, the Azure DevOps write and the outbox (ADR 0017).
/// </summary>
public static class Correlation
{
    /// <summary>The prefix of the short form written into Azure DevOps comments.</summary>
    public const string TagPrefix = "zsa-";

    /// <summary>The number of trace id characters in the short form.</summary>
    public const int ShortLength = 12;

    /// <summary>Creates a new correlation id.</summary>
    /// <returns>A W3C trace id.</returns>
    public static string New() => ActivityTraceId.CreateRandom().ToHexString();

    /// <summary>
    /// Derives the correlation id for a session saved before correlation existed, so old data still has a stable, valid one.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <returns>A W3C trace id derived from the session id.</returns>
    public static string ForLegacySession(Guid sessionId) => sessionId.ToString("N");

    /// <summary>Checks that a value is a well-formed trace id.</summary>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> for 32 lower-case hex characters that are not all zero.</returns>
    public static bool IsValid(string? value) =>
        value is { Length: 32 } && value.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')) && value.Any(c => c != '0');

    /// <summary>Gets the short form written at the end of every Azure DevOps comment the agent writes.</summary>
    /// <param name="correlationId">The full correlation id.</param>
    /// <returns>For example <c>zsa-4bf92f3577b3</c>.</returns>
    public static string Tag(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        return TagPrefix + correlationId[..Math.Min(ShortLength, correlationId.Length)];
    }

    /// <summary>Normalises a reference given by a person: the full id or the <c>zsa-</c> short form.</summary>
    /// <param name="reference">What the caller typed.</param>
    /// <returns>The trace id prefix to match (at least the short length), or <see langword="null"/> when it is not a usable reference.</returns>
    public static string? ToPrefix(string? reference)
    {
        var value = reference?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.StartsWith(TagPrefix, StringComparison.Ordinal))
        {
            value = value[TagPrefix.Length..];
        }

        return value.Length is >= ShortLength and <= 32 && value.All(Uri.IsHexDigit) ? value : null;
    }
}
