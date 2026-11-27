using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.WorkSignals;

/// <summary>Configurable retention and excerpt bounds, checked before evidence can be displayed or stored.</summary>
/// <param name="Retention">Maximum source age and reference retention; positive and at most thirty days.</param>
/// <param name="MaxSnippetCharacters">Maximum short excerpt length, between one and one thousand characters.</param>
public sealed record WorkSignalEvidencePolicy(TimeSpan Retention, int MaxSnippetCharacters)
{
    /// <summary>Rejects unscoped, stale, future, oversized or unlinked evidence; never silently treats it as no results.</summary>
    /// <param name="evidence">Provider evidence.</param>
    /// <param name="scope">Code-verified sources.</param>
    /// <param name="source">The expected source type for this operation.</param>
    /// <param name="resourceId">The exact requested channel or meeting.</param>
    /// <param name="now">The clock timestamp.</param>
    /// <returns>Success, or a visible invalid-evidence error.</returns>
    public Result Validate(IReadOnlyList<WorkSignalEvidence> evidence, WorkSignalScope scope,
        WorkSignalSource source, string resourceId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(scope);
        if (Retention <= TimeSpan.Zero || Retention > TimeSpan.FromDays(30) || MaxSnippetCharacters is < 1 or > 1000)
        {
            return Result.Failure(Error.Invalid("signals.policy_invalid", "Signal retention and excerpt limits are invalid."));
        }

        foreach (var item in evidence)
        {
            if (item.Source != source || item.ResourceId != resourceId ||
                source == WorkSignalSource.Channel && item.TeamsGroupId != scope.TeamsGroupId || !item.Link.IsAbsoluteUri ||
                item.Link.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(item.Title) ||
                string.IsNullOrWhiteSpace(item.Snippet) || item.Snippet.Length > MaxSnippetCharacters ||
                item.Timestamp > now || item.Timestamp <= now - Retention ||
                item.MemberObjectId is { } member && !scope.RosterObjectIds.Contains(member))
            {
                return Result.Failure(Error.Conflict("signals.evidence_rejected", "No signals available. Work IQ returned evidence outside the verified source, roster or retention limits."));
            }
        }

        return Result.Success();
    }
}
