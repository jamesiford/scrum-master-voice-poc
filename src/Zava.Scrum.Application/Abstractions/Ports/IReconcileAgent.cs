using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Read-only agent that compares a spoken update with a member's brief (SPEC 10.2).</summary>
public interface IReconcileAgent
{
    /// <summary>Reconciles an update.</summary>
    /// <param name="brief">The member's brief.</param>
    /// <param name="utterance">What they said, transcribed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="recorded">Earlier statements by this member, offered when the member is correcting themselves.</param>
    /// <returns>A suggestion, or <see langword="null"/> when the model is unavailable.</returns>
    Task<ReconcileSuggestion?> ReconcileAsync(MemberBrief brief, string utterance, CancellationToken ct, IReadOnlyList<RecordedStatement>? recorded = null);
}
